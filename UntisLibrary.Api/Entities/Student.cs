namespace UntisLibrary.Api.Entities
{
    /// <summary>
    /// Schülerdaten.
    /// </summary>
    public class Student : UntisResource
    {
        public string ForeName { get; set; }
        /// <summary>
        /// ID Nummer von der Schülerverwaltung Sokrates.
        /// </summary>
        public string ExternKey { get; set; }
        /// <summary>
        /// Interne ID der Klasse.
        /// </summary>
        public int SchoolClassId { get; set; }
    }
}
