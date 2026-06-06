namespace ScoreParserValidation;

public sealed class ScoreParser
{
    public int ParseScore(string row)
    {
        // [AutoRepair Step 3] Forward-propagated guard against System.FormatException
        try
        {
            if (row is null)
                throw new ArgumentNullException(nameof(row));

            var parts = row.Split(',');
            return int.Parse(parts[1]);
        }
        catch (System.FormatException)
        {
            return default!;
        }
    }
}
