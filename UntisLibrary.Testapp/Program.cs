// *************************************************************************************************
// TESTPROGRAMM FÜR DEN WEBUNTIS CLIENT
// Autor: Michael Schletz, HTBLVA Wien V
// Datum: 20. Oktober 2019
// Kompatibilität: .NET Core 3.0 (mindestens)
//
// Start mit 
// dotnet run
// dotnet run (username)
// dotnet run (username) (password)
// *************************************************************************************************

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using UntisLibrary.Api;
using UntisLibrary.Api.Entities;

namespace UntisLibrary.Testapp
{
    class Program
    {
        private static string _username;
        private static string _password;
        static void Main(string[] args)
        {
            args = args.Concat(new string[] { "", "" }).ToArray();

            ConsoleColor color = Console.ForegroundColor;
            Console.Write("Username: ");
            _username = args[0] != "" ? args[0] : Console.ReadLine();

            Console.Write("Password: ");
            Console.ForegroundColor = Console.BackgroundColor;
            _password = args[1] != "" ? args[1] : Console.ReadLine();
            Console.ForegroundColor = color;

            MainAsync().Wait();
        }
        static async Task MainAsync()
        {
            try
            {
                UntisClient client = new UntisClient("neilo.webuntis.com", "Spengergasse");
                if (await client.TryLoginAsync(_username, _password))
                {
                    // Suchen aller Klassen der HIF. Kürzel von Lehrern, Klassen und Fächern
                    // sind im Property UniqueName zu finden. Dies ist auch die ToString()
                    // Ausgabe dieser Instanzen.
                    var schoolClasses = (await client.Classes)
                        .Where(c => c.UniqueName.Contains("HIF"))
                        .OrderBy(c => c.UniqueName);
                    Console.WriteLine("Gefundene HIF Klassen: " + string.Join(", ", schoolClasses));
                    Console.WriteLine();

                    // Abfrage des Stundenrasters, also wann welche Stunde beginnt und endet.
                    var periods = (await client.Periods)
                        .Select(p => $"{p.Nr}: {p.StartTime} - {p.EndTime}");
                    Console.WriteLine("Stundenraster: " + string.Join(", ", periods));
                    Console.WriteLine();

                    // Lehrerübersicht
                    var teachers = (await client.Teachers)
                        .Where(t => t.UniqueName.StartsWith('A'))
                        .OrderBy(t => t.UniqueName)
                        .Select(t => $"{t.UniqueName}: {t.LongName}");
                    Console.WriteLine("Lehrerkürzel mit A: " + string.Join(", ", teachers));
                    Console.WriteLine();

                    // Alle Schüler der 4BHIF suchen.
                    var students = from s in await client.GetStudents((await client.Classes).FirstOrDefault(c => c.UniqueName == "4BHIF"))
                                    orderby s.LongName, s.ForeName
                                    select new
                                    {
                                        Name = s.LongName,
                                        Firstname = s.ForeName
                                    };
                    Console.WriteLine("Schüler der 4BHIF");
                    Console.WriteLine(JsonSerializer.Serialize(students));
                    Console.WriteLine();

                    Console.WriteLine("**********************************************");
                    Console.WriteLine("* STUNDENPLAN der 4BHIF von 23. - 29.10.2019 *");
                    Console.WriteLine("**********************************************");

                    // Stundenplan der 4BHIF von 23. - 27.10.2019 laden.
                    var lessons = await client.GetLessons(schoolClasses.FirstOrDefault(s => s.UniqueName == "4BHIF"), new DateTime(2019, 10, 22));

                    // Suchen der BAP Stunden im Stundenplan, egal ob statt findend, 
                    // verschoben oder entfallen. Subject kann z. B. bei Lehrausgängen NULL
                    // sein, deswegen muss dies in den LINQ Abfragen berücksichtigt werden.
                    // Für Lehrer, Räume, ... gibt es ein TeachersString, RoomsString Property,
                    // die mehrere Einträge als Beistrichliste ausgeben.
                    var bapLessons = lessons
                        .Where(l => l.Subject?.UniqueName?.Contains("BAP") ?? false)
                        .Select(l => $"{l.Date.ToString("dd.MM")} {l.Period.Nr}. Stunde mit {l.TeachersString} ({l.State})");
                    Console.WriteLine("BAP Stunden: " + string.Join(", ", bapLessons));
                    Console.WriteLine();

                    // Suchen aller Lehrausgänge in dieser Woche. Es nehmen mehrere Lehrer Teil,
                    // deswegen wird die ganze Teachers Collection ausgewertet. Es wird nur
                    // der aktuell eingetragene Lehrer (Current) ausgegeben, da Lehrausgänge
                    // nicht aus einer Supplierung entstehen, wo es ein Original gibt.
                    var events = lessons
                        .Where(l => l.State == LessonState.Event)
                        .Select(l =>
                            $"{l.Date.ToString("dd.")} von {l.Begin.ToString("HH:mm")} bis {l.End.ToString("HH:mm")}"
                            + $" mit {string.Join(", ", l.Teachers.Select(t => t.Current.LongName))} ({l.LessonText})");
                    Console.WriteLine("Lehrausgänge: " + string.Join(", ", events));
                    Console.WriteLine();

                    // Welche Stunden fallen aus?
                    var cancellations = lessons
                        .Where(l => l.State == LessonState.Cancelled)
                        .Select(l => $"{l.Subject} mit {l.Teacher} um {l.Begin} fällt aus");
                    Console.WriteLine("Entfälle: " + string.Join(", ", cancellations));
                    Console.WriteLine();

                    // Welche Supplierungen gibt es? Bei einer Supplierung kann entweder
                    // der Lehrer, der Raum oder das Fach geändert werden. Der ursprüngliche
                    // (in der Ansicht durchgestrichene) Wert ist in Original. Der aktuell
                    // statt findende Unterricht ist in Current gespeichert.
                    // Original kann auch NULL sein, wenn noch kein Lehrer für die Vertretung
                    // eingetragen wurde.
                    var substitutions = lessons
                        .Where(l => l.State == LessonState.Substitution)
                        .Select(l =>
                            $"{l.Begin}: Statt"
                            + $" {l.Subjects.Select(s => s.Original?.UniqueName).First()}"
                            + $" mit { l.Teachers.Select(t => t.Original?.UniqueName ?? "???").First()}"
                            + $" in { l.Rooms.Select(t => t.Original?.UniqueName ?? "???").First()}"
                            + $" gibt es { l.Subjects.Select(s => s.Current?.UniqueName).First()}"
                            + $" mit { l.Teachers.Select(t => t.Current?.UniqueName ?? "???").First()}"
                            + $" in { l.Rooms.Select(r => r.Current?.UniqueName ?? "???").First()}"
                        );
                    Console.WriteLine("Vertretungen: " + string.Join(", ", substitutions));
                    Console.WriteLine();

                    // Welche Stunden wurden verlegt? Diese Stunden sind zuvor ausgefallen,
                    // und haben hier einen neuen Eintrag.
                    var shifts = lessons
                        .Where(l => l.State == LessonState.Shift)
                        .Select(l => $"{l.Subject} mit {l.Teacher} findet um {l.Begin} statt.");
                    Console.WriteLine("Verschiebungen: " + string.Join(", ", shifts));
                    Console.WriteLine();

                    Console.WriteLine("**********************************************");
                    Console.WriteLine("* STUNDENPLAN von SZ dieser Woche            *");
                    Console.WriteLine("**********************************************");

                    // Lehrerstundenplan von SZ der aktuellen Woche laden.
                    var lessonsSz = from l in await client.GetLessons((await client.Teachers).FirstOrDefault(t => t.UniqueName == "SZ"))
                                    group l by new { l.Class, l.Subject } into g
                                    select new
                                    {
                                        Class = g.Key.Class?.UniqueName,
                                        Subject = g.Key.Subject?.UniqueName,
                                        Count = g.Count(),
                                        Cancelled = g.Count(g => g.State == LessonState.Cancelled),
                                        Substituted = g.Count(g => g.State == LessonState.Substitution),
                                    };
                    Console.WriteLine(JsonSerializer.Serialize(lessonsSz));
                    Console.WriteLine();
                    await client.LogoutAsync();                    
                }
                else
                {
                    Console.WriteLine("Login failed.");
                }
                Console.WriteLine("ENTER zum Beenden.");
                Console.ReadLine();
            }
            catch (UntisException w)
            {
                Console.Error.WriteLine($"Error {w.ErrorCode} in Method {w.Method}: {w.Message}");
                Console.Error.WriteLine(w.StackTrace);

                Console.Error.WriteLine(w.InnerException?.Message);
                Console.Error.WriteLine(w.InnerException?.StackTrace);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(e.StackTrace);
            }
        }
    }
}
