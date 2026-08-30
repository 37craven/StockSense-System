namespace StockSense.Client.Components;

public sealed class SortState
{
    public string Column { get; set; } = "";
    public bool Ascending { get; set; }

    public void Toggle(string column)
    {
        if (Column == column) Ascending = !Ascending;
        else { Column = column; Ascending = true; }
    }

    public string Arrow(string column) =>
        Column == column ? (Ascending ? " ▲" : " ▼") : "";
}
