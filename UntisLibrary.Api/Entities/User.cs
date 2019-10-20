namespace UntisLibrary.Api.Entities
{
    /// <summary>
    /// Angemeldeter Benutzer.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Benutzername im ActiveDirectory und somit auch in WebUntis.
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// Vom Login zurückgeliefertes JSESSIONID Cookie.
        /// </summary>
        public string SessionId { get; set; }
        /// <summary>
        /// Bestimmt, ob der Benutzer Schüler oder Lehrer ist.
        /// </summary>
        public ResourceType PersonType { get; set; }
        /// <summary>
        /// Interne ID der Person.
        /// </summary>
        public int PersonId { get; set; }
        /// <summary>
        /// Interne ID der Klasse bei Schülern.
        /// </summary>
        public int KlasseId { get; set; }
    }
}