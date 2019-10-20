namespace UntisLibrary.Api.Entities
{
    /// <summary>
    /// Angaben zum Raum, sofern sie im Untis System eingetragen wurden.
    /// </summary>
    public class Room : UntisResource
    {
        public string Description { get; set; }
        public int Capacity { get; set; }
    }
}
