using System.Globalization;
using Microsoft.VisualBasic.FileIO;

namespace DesktopUtility.Core.Monitoring;

public sealed class PresentMonFrameRates(int processId)
{
    private readonly Dictionary<string, (int Frames, double Milliseconds)> streams = new(StringComparer.OrdinalIgnoreCase);
    private readonly object gate = new();
    private int processColumn = -1;
    private int streamColumn = -1;
    private int durationColumn = -1;
    private int droppedColumn = -1;

    public void AddLine(string line)
    {
        var cells = Split(line);

        if (cells.Length == 0)
        {

            return;
        }

        int id = Array.IndexOf(cells, "ProcessID");
        int swapChain = Array.IndexOf(cells, "SwapChainAddress");

        if (id >= 0 && swapChain >= 0)
        {
            processColumn = id;
            streamColumn = swapChain;
            droppedColumn = Array.IndexOf(cells, "Dropped");
            durationColumn = Array.IndexOf(cells, "DisplayedTime");

            if (durationColumn < 0)
            {
                durationColumn = Array.IndexOf(cells, "MsBetweenDisplayChange");
            }

            return;
        }

        int lastColumn = Math.Max(Math.Max(processColumn, streamColumn), Math.Max(durationColumn, droppedColumn));

        if (processColumn < 0 || durationColumn < 0 || cells.Length <= lastColumn
            || !int.TryParse(cells[processColumn], NumberStyles.Integer, CultureInfo.InvariantCulture, out int pid)
            || pid != processId || (droppedColumn >= 0 && cells[droppedColumn] == "1")
            || !double.TryParse(cells[durationColumn], NumberStyles.Float, CultureInfo.InvariantCulture, out double duration)
            || !double.IsFinite(duration) || duration <= 0)
        {

            return;
        }

        lock (gate)
        {
            string address = cells[streamColumn];
            var sample = streams.GetValueOrDefault(address);
            streams[address] = (sample.Frames + 1, sample.Milliseconds + duration);
        }
    }

    public double? TakeFramesPerSecond()
    {
        lock (gate)
        {
            var busiest = streams.Values.OrderByDescending(value => value.Frames).ThenByDescending(value => value.Milliseconds)
                .FirstOrDefault();
            streams.Clear();

            return busiest.Frames == 0 ? null : 1000 * busiest.Frames / busiest.Milliseconds;
        }
    }

    private static string[] Split(string line)
    {

        if (!line.Contains('"'))
        {

            return line.Split(',');
        }

        using var parser = new TextFieldParser(new StringReader(line));
        parser.SetDelimiters(",");
        try
        {

            return parser.ReadFields() ?? [];
        }
        catch (MalformedLineException)
        {

            return [];
        }
    }
}
