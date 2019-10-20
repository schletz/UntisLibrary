namespace UntisLibrary.Api.Entities
{
    public enum ResourceType
    {
        SchoolClass = 1,
        Teacher = 2,
        Subject = 3,
        Room = 4,
        Student = 5,
        Timetable = 6
    }
    /// <summary>
    /// Basisklasse mit den Properties für alle Ressourcen (Räume, Lehrer, Fächer, ...)
    /// </summary>
    public class UntisResource
    {
        public ResourceType Type { get; set; }
        public int InternalId { get; set; }
        /// <summary>
        /// Kürzel
        /// </summary>
        public string UniqueName { get; set; }
        /// <summary>
        /// Wird manchmal gesetzt.
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Detaillierte Beschreibung oder Zuname bei Personen.
        /// </summary>
        public string LongName { get; set; }
        /// <summary>
        /// Wird manchmal bei Fächern oder Klassen mit Schwerpunkten in der 
        /// Fachschule gesetzt.
        /// </summary>
        public string AlternateName { get; set; }
        /// <summary>
        /// Gibt das Kürzel als Stringrepräsentation aus.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => UniqueName;
    }
}
