using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AutoRepair.Step3Repair.Models;

namespace AutoRepair.Step3Repair.Analysis;

/// <summary>
/// Builds a simplified Control Flow Graph for each method/accessor whose line range
/// overlaps with at least one violation line.  The CFG captures branches, loops,
/// try/catch, return/throw, break/continue and sequential flow.
/// </summary>
public sealed class CfgBuilder
{
    private int _nextId;
    private List<CfgNode> _nodes = null!;
    private List<CfgEdge> _edges = null!;

    // ───────────────────────── public API ─────────────────────────

    public List<MethodCfg> BuildForFile(string fullPath, string relativePath, List<int> violationLines)
    {
        var source = File.ReadAllText(fullPath);
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();
        var result = new List<MethodCfg>();

        foreach (var member in root.DescendantNodes())
        {
            string? name;
            BlockSyntax? body;
            ArrowExpressionClauseSyntax? exprBody;

            switch (member)
            {
                case MethodDeclarationSyntax m:
                    name = m.Identifier.Text;
                    body = m.Body;
                    exprBody = m.ExpressionBody;
                    break;
                case ConstructorDeclarationSyntax c:
                    name = c.Identifier.Text + "..ctor";
                    body = c.Body;
                    exprBody = null;
                    break;
                case AccessorDeclarationSyntax a:
                    name = AccessorName(a);
                    body = a.Body;
                    exprBody = a.ExpressionBody;
                    break;
                default:
                    continue;
            }

            var span = member.GetLocation().GetLineSpan();
            int startLine = span.StartLinePosition.Line + 1;
            int endLine   = span.EndLinePosition.Line + 1;

            var relevant = violationLines.Where(l => l >= startLine && l <= endLine).ToList();
            if (relevant.Count == 0) continue;

            // Reset per-method state
            _nextId = 0;
            _nodes = new List<CfgNode>();
            _edges = new List<CfgEdge>();

            var entry = Node("entry", startLine, startLine, $"Enter {name}");
            var exit  = Node("exit",  endLine,   endLine,   $"Exit {name}");

            if (body is { Statements.Count: > 0 })
            {
                var (first, last) = Sequence(body.Statements, exit.Id, null);
                if (first >= 0) Edge(entry.Id, first, "sequential");
                if (last  >= 0) Edge(last, exit.Id, "fall-through");
            }
            else if (exprBody != null)
            {
                var expr = NodeSyn("expression", exprBody.Expression);
                Edge(entry.Id, expr.Id, "sequential");
                Edge(expr.Id,  exit.Id, "sequential");
            }
            else
            {
                Edge(entry.Id, exit.Id, "empty");
            }

            result.Add(new MethodCfg(
                MethodName:      name,
                File:            relativePath,
                MethodStartLine: startLine,
                MethodEndLine:   endLine,
                ViolationLines:  relevant,
                Nodes:           _nodes.ToList(),
                Edges:           _edges.ToList()));
        }

        return result;
    }

    // ───────────────────── CFG primitives ──────────────────────

    private CfgNode Node(string kind, int startLine, int endLine, string code)
    {
        var n = new CfgNode(_nextId++, kind, startLine, endLine, Trunc(code, 150));
        _nodes.Add(n);
        return n;
    }

    private CfgNode NodeSyn(string kind, SyntaxNode syn)
    {
        var sp = syn.GetLocation().GetLineSpan();
        return Node(kind,
            sp.StartLinePosition.Line + 1,
            sp.EndLinePosition.Line + 1,
            Collapse(syn.ToString()));
    }

    private void Edge(int from, int to, string label)
    {
        if (from >= 0 && to >= 0)
            _edges.Add(new CfgEdge(from, to, label));
    }

    // ────────────────── statement builders ─────────────────────
    // Each returns (firstNodeId, lastNodeId).
    // lastNodeId == -1 means the path is unreachable after this point (return/throw/break/continue).

    private (int first, int last) Sequence(SyntaxList<StatementSyntax> stmts, int exitId, Loop? loop)
    {
        if (stmts.Count == 0) return (-1, -1);

        int firstId = -1, prev = -1;
        foreach (var s in stmts)
        {
            var (f, l) = Stmt(s, exitId, loop);
            if (f < 0) continue;
            if (firstId < 0) firstId = f;
            if (prev >= 0) Edge(prev, f, "sequential");
            prev = l;
            if (l < 0) break; // unreachable from here on
        }
        return (firstId, prev);
    }

    private (int first, int last) Stmt(StatementSyntax s, int exitId, Loop? loop) => s switch
    {
        BlockSyntax              x => Sequence(x.Statements, exitId, loop),
        IfStatementSyntax        x => If(x, exitId, loop),
        WhileStatementSyntax     x => While(x, exitId),
        ForStatementSyntax       x => For(x, exitId),
        ForEachStatementSyntax   x => ForEach(x, exitId),
        DoStatementSyntax        x => Do(x, exitId),
        SwitchStatementSyntax    x => Switch(x, exitId, loop),
        TryStatementSyntax       x => Try(x, exitId, loop),
        ReturnStatementSyntax    x => Return(x, exitId),
        ThrowStatementSyntax     x => Throw(x, exitId),
        BreakStatementSyntax     x => Break(x, loop),
        ContinueStatementSyntax  x => Continue(x, loop),
        _                          => Simple(s),
    };

    private (int f, int l) Simple(StatementSyntax s)
    {
        var n = NodeSyn("statement", s);
        return (n.Id, n.Id);
    }

    private (int f, int l) Return(ReturnStatementSyntax s, int exitId)
    {
        var n = NodeSyn("return", s);
        Edge(n.Id, exitId, "return");
        return (n.Id, -1);
    }

    private (int f, int l) Throw(ThrowStatementSyntax s, int exitId)
    {
        var n = NodeSyn("throw", s);
        Edge(n.Id, exitId, "throw");
        return (n.Id, -1);
    }

    private (int f, int l) Break(BreakStatementSyntax s, Loop? loop)
    {
        var n = NodeSyn("break", s);
        if (loop != null) Edge(n.Id, loop.ExitId, "break");
        return (n.Id, -1);
    }

    private (int f, int l) Continue(ContinueStatementSyntax s, Loop? loop)
    {
        var n = NodeSyn("continue", s);
        if (loop != null) Edge(n.Id, loop.HeaderId, "continue");
        return (n.Id, -1);
    }

    // ─── if / else ───

    private (int f, int l) If(IfStatementSyntax s, int exitId, Loop? loop)
    {
        var br   = NodeSyn("branch", s.Condition);
        var join = Node("join", EndL(s), EndL(s), "endif");

        // true branch
        var (tf, tl) = Stmt(s.Statement, exitId, loop);
        if (tf >= 0) { Edge(br.Id, tf, "true"); if (tl >= 0) Edge(tl, join.Id, "sequential"); }
        else         Edge(br.Id, join.Id, "true");

        // false / else branch
        if (s.Else != null)
        {
            var (ff, fl) = Stmt(s.Else.Statement, exitId, loop);
            if (ff >= 0) { Edge(br.Id, ff, "false"); if (fl >= 0) Edge(fl, join.Id, "sequential"); }
            else         Edge(br.Id, join.Id, "false");
        }
        else Edge(br.Id, join.Id, "false");

        return (br.Id, join.Id);
    }

    // ─── while ───

    private (int f, int l) While(WhileStatementSyntax s, int exitId)
    {
        var hdr = NodeSyn("loop-header", s.Condition);
        var ex  = Node("loop-exit", EndL(s), EndL(s), "end-while");
        Edge(hdr.Id, ex.Id, "false");

        var ctx = new Loop(hdr.Id, ex.Id);
        var (bf, bl) = Stmt(s.Statement, exitId, ctx);
        if (bf >= 0) { Edge(hdr.Id, bf, "true"); if (bl >= 0) Edge(bl, hdr.Id, "back-edge"); }

        return (hdr.Id, ex.Id);
    }

    // ─── for ───

    private (int f, int l) For(ForStatementSyntax s, int exitId)
    {
        int initF = -1, initL = -1;
        if (s.Declaration != null)
        {
            var n = NodeSyn("for-init", s.Declaration);
            initF = initL = n.Id;
        }

        var hdr = s.Condition != null
            ? NodeSyn("loop-header", s.Condition)
            : Node("loop-header", StartL(s), StartL(s), "for(;;)");
        if (initL >= 0) Edge(initL, hdr.Id, "sequential");

        var ex = Node("loop-exit", EndL(s), EndL(s), "end-for");
        Edge(hdr.Id, ex.Id, "false");

        CfgNode? inc = s.Incrementors.Count > 0 ? NodeSyn("for-update", s.Incrementors[0]) : null;

        var ctx = new Loop(hdr.Id, ex.Id);
        var (bf, bl) = Stmt(s.Statement, exitId, ctx);
        if (bf >= 0)
        {
            Edge(hdr.Id, bf, "true");
            if (bl >= 0)
            {
                if (inc != null) { Edge(bl, inc.Id, "sequential"); Edge(inc.Id, hdr.Id, "back-edge"); }
                else             Edge(bl, hdr.Id, "back-edge");
            }
        }

        return (initF >= 0 ? initF : hdr.Id, ex.Id);
    }

    // ─── foreach ───

    private (int f, int l) ForEach(ForEachStatementSyntax s, int exitId)
    {
        var code = $"foreach ({s.Type} {s.Identifier} in ...)";
        var hdr  = Node("loop-header", StartL(s), StartL(s), code);
        var ex   = Node("loop-exit", EndL(s), EndL(s), "end-foreach");
        Edge(hdr.Id, ex.Id, "false");

        var ctx = new Loop(hdr.Id, ex.Id);
        var (bf, bl) = Stmt(s.Statement, exitId, ctx);
        if (bf >= 0) { Edge(hdr.Id, bf, "true"); if (bl >= 0) Edge(bl, hdr.Id, "back-edge"); }

        return (hdr.Id, ex.Id);
    }

    // ─── do-while ───

    private (int f, int l) Do(DoStatementSyntax s, int exitId)
    {
        var cond = NodeSyn("loop-condition", s.Condition);
        var ex   = Node("loop-exit", EndL(s), EndL(s), "end-do");

        var ctx = new Loop(cond.Id, ex.Id);
        var (bf, bl) = Stmt(s.Statement, exitId, ctx);

        int entry = bf >= 0 ? bf : cond.Id;
        if (bl >= 0) Edge(bl, cond.Id, "sequential");
        if (bf >= 0) Edge(cond.Id, bf, "true");  // back-edge
        Edge(cond.Id, ex.Id, "false");

        return (entry, ex.Id);
    }

    // ─── switch ───

    private (int f, int l) Switch(SwitchStatementSyntax s, int exitId, Loop? loop)
    {
        var sw   = NodeSyn("switch", s.Expression);
        var join = Node("join", EndL(s), EndL(s), "end-switch");

        // break inside a switch section should exit the switch (not an enclosing loop)
        var ctx = new Loop(loop?.HeaderId ?? -1, join.Id);

        foreach (var sec in s.Sections)
        {
            var lbl = string.Join(", ", sec.Labels.Select(l => Collapse(l.ToString())));
            var cn  = Node("case", StartL(sec), StartL(sec), Trunc(lbl, 150));
            Edge(sw.Id, cn.Id, "case");

            var (bf, bl) = Sequence(sec.Statements, exitId, ctx);
            if (bf >= 0) { Edge(cn.Id, bf, "sequential"); if (bl >= 0) Edge(bl, join.Id, "fall-through"); }
            else         Edge(cn.Id, join.Id, "sequential");
        }

        return (sw.Id, join.Id);
    }

    // ─── try / catch / finally ───

    private (int f, int l) Try(TryStatementSyntax s, int exitId, Loop? loop)
    {
        var tryN = Node("try", StartL(s), StartL(s), "try");
        var join = Node("join", EndL(s), EndL(s), "end-try");

        // try body
        var (tf, tl) = Sequence(s.Block.Statements, exitId, loop);
        if (tf >= 0) { Edge(tryN.Id, tf, "try-body"); if (tl >= 0) Edge(tl, join.Id, "sequential"); }
        else         Edge(tryN.Id, join.Id, "try-body");

        // catch clauses
        foreach (var c in s.Catches)
        {
            var ct = c.Declaration?.Type.ToString() ?? "Exception";
            var cn = Node("catch", StartL(c), StartL(c), $"catch ({ct})");
            Edge(tryN.Id, cn.Id, "exception");

            var (cf, cl) = Sequence(c.Block.Statements, exitId, loop);
            if (cf >= 0) { Edge(cn.Id, cf, "sequential"); if (cl >= 0) Edge(cl, join.Id, "sequential"); }
            else         Edge(cn.Id, join.Id, "sequential");
        }

        // finally
        if (s.Finally != null)
        {
            var fin     = Node("finally", StartL(s.Finally), StartL(s.Finally), "finally");
            var endJoin = Node("join", EndL(s), EndL(s), "end-finally");
            Edge(join.Id, fin.Id, "finally");

            var (ff, fl) = Sequence(s.Finally.Block.Statements, exitId, loop);
            if (ff >= 0) { Edge(fin.Id, ff, "sequential"); if (fl >= 0) Edge(fl, endJoin.Id, "sequential"); }
            else         Edge(fin.Id, endJoin.Id, "sequential");

            return (tryN.Id, endJoin.Id);
        }

        return (tryN.Id, join.Id);
    }

    // ───────────────────── utilities ──────────────────────────

    private static int StartL(SyntaxNode n) => n.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
    private static int EndL(SyntaxNode n)   => n.GetLocation().GetLineSpan().EndLinePosition.Line + 1;

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ").Trim();
    private static string Trunc(string text, int max) => text.Length > max ? text[..(max - 3)] + "..." : text;

    private static string AccessorName(AccessorDeclarationSyntax a)
    {
        var prop = a.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
        return $"{prop?.Identifier.Text ?? "property"}.{a.Keyword.Text}";
    }

    private sealed record Loop(int HeaderId, int ExitId);
}
