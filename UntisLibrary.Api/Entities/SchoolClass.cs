namespace UntisLibrary.Api.Entities
{
    /// <summary>
    /// Angaben zu einer Klasse.
    /// </summary>
    public class SchoolClass : UntisResource
    {
        public string Description { get; set; }
        /// <summary>
        /// Klassenvorstand. Kann auch NULL sein.
        /// </summary>
        public Teacher ClassTeacher { get; set; }
    }
}