using System;

namespace UntisLibrary.Api.Entities
{
    /// <summary>
    /// Zeitpunkt einer Unterrichtsstunde im Stundenraster der Schule.
    /// </summary>
    public class Period
    {
        public int Nr { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public override string ToString() => Nr.ToString();
    }
}
