namespace ScoreParserValidation;

public sealed class ScoreParser
{
    public int ParseScore(string row)
    {
        if (row is null)
            throw new ArgumentNullException(nameof(row));

        var parts = row.Split(',');
        return int.Parse(parts[1]);
    }
}
