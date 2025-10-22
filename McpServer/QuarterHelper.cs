using System.Text;

namespace McpServer;

public static class QuarterHelper
{
    public static (string, string) RetrieveQuarterRangeInTheYear(int quarter, int year)
    {
        if (quarter < 1 || quarter > 4)
        {
            throw new InvalidOperationException("Quarter must be between 1 and 4.");
        }

        StringBuilder start = new StringBuilder(year.ToString());
        StringBuilder end = new StringBuilder(year.ToString());
        if (quarter == 1)
        {
            start.Append("-01-01");
            end.Append("-03-31");
        } else if (quarter == 2)
        {
            start.Append("-04-01");
            end.Append("-06-30");
        } else if (quarter == 3)
        {
            start.Append("-07-01");
            end.Append("-09-30");
        } else
        {
            start.Append("-10-01");
            end.Append("-12-31");
        }
        return (start.ToString(), end.ToString());
    }
}