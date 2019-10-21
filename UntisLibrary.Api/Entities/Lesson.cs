using System;
using System.Collections.Generic;
using System.Linq;

namespace UntisLibrary.Api.Entities
{
    /// <summary>
    /// Status der Unterrichtsstunde, ob sie entfällt, vertreten oder verschoben wird oder
    /// ob sie ein Lehrausgang ist.
    /// </summary>
    public enum LessonState
    {
        Other = 0,
        Standard = 1,
        Cancelled = 2,
        Substitution = 3,
        Event = 4,
        Shift = 5
    }

    /// <summary>
    /// Von der Unterrichtsstunde verwendete Resourcen.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LessonResource<T>
    {
        /// <summary>
        /// Aktuell im Stundenplan verwendeter Lehrer, Raum, ...
        /// </summary>
        public T Current { get; set; }
        /// <summary>
        /// Ursprünglich geplanter Lehrer, Raum, ... der aber geändert werden kann.
        /// Wird keine Änderung durchgeführt, ist die gleiche Instanz der Resource
        /// von Current auch in Original.
        /// </summary>
        public T Original { get; set; }
    }

    /// <summary>
    /// Bildet eine einzelne Stunde im Stundenplan ab. Diese ist nicht wöchentlich,
    /// sondern findet an einem bestimmten Datum statt.
    /// </summary>
    public class Lesson
    {
        public Period Period { get; set; }              // Zeitpunkt des Unterrichtes
        public string StudentGroup { get; set; }        // z. B. AMx_1AHIF, ...
        public string LessonText { get; set; }          // Anmerkungen (Lehrausgang, ...)
        public string PeriodText { get; set; }
        public LessonState State { get; set; }
        public DateTime Begin { get; set; }            // Beginn mit Datum
        public DateTime End { get; set; }              // Ende mit Datum
        public DateTime Date => Begin.Date;
        public int Weekday => (int) Begin.DayOfWeek + 1;
        /// <summary>
        /// In dieser Stunde eingetragene Klassen. Das können auch mehrere sein (RISL, BAP, ...)
        /// </summary>
        public IEnumerable<LessonResource<SchoolClass>> Classes { get; set; }
        /// <summary>
        /// Die aktuell eingetragenen Klassen als Beistrichliste.
        /// </summary>
        public string ClassesString => string.Join(",", Classes.Select(c => c.Current?.UniqueName));
        /// <summary>
        /// Gibt die erste Klasse dieser Stunde zurück. Da meist nur eine Klasse vorhanden 
        /// ist kommt man oft mit diesem Property aus.
        /// </summary>
        public SchoolClass Class => Classes.Select(c => c.Current).DefaultIfEmpty().First();
        /// <summary>
        /// In dieser Stunde eingetragene Lehrer. Dies können mehrere sein, wenn 2 Lehrer eine
        /// Stunde unterrichten.
        /// </summary>
        public IEnumerable<LessonResource<Teacher>> Teachers { get; set; }
        /// <summary>
        ///  Die aktuell eingetragenen Lehrer als Beistrichliste.
        /// </summary>
        public string TeachersString => string.Join(",", Teachers.Select(c => c.Current?.UniqueName));
        /// <summary>
        /// Gibt den ersten Lehrer dieser Stunde zurück.
        /// </summary>
        public Teacher Teacher => Teachers.Select(c => c.Current).DefaultIfEmpty().First();
        /// <summary>
        /// In dieser Stunde eingetragene Fächer. Theoretisch können es mehrere sein, es gibt aber
        /// keine praktische Anwendung von mehreren Fächern in einer Stunde.
        /// </summary>
        public IEnumerable<LessonResource<Subject>> Subjects { get; set; }
        /// <summary>
        ///  Die aktuell eingetragenen Fächer als Beistrichliste.
        /// </summary>
        public string SubjectsString => string.Join(",", Subjects.Select(c => c.Current?.UniqueName));
        /// <summary>
        /// Gibt das erste Fach dieser Stunde zurück.
        /// </summary>
        public Subject Subject => Subjects.Select(c => c.Current).DefaultIfEmpty().First();
        /// <summary>
        /// Von dieser Stunde betroffene Räume. Manchmal ist ein Labor und ein Stammraum bzw.
        /// zwei Labore (NVS) eingetragen.
        /// </summary>
        public IEnumerable<LessonResource<Room>> Rooms { get; set; }
        /// <summary>
        ///  Die aktuell eingetragenen Räume als Beistrichliste.
        /// </summary>
        public string RoomsString => string.Join(",", Rooms.Select(c => c.Current?.UniqueName));
        /// <summary>
        /// Gibt den ersten Raum dieser Stunde zurück.
        /// </summary>
        public Room Room => Rooms.Select(c => c.Current).DefaultIfEmpty().First();

    }
}
