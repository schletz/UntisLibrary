// *************************************************************************************************
// CLIENT FÜR WEBUNTIS
// Autor: Michael Schletz, HTBLVA Wien V
// Datum: 20. Oktober 2019
// Kompatibilität: .NET Core 3.0 (mindestens wegen System.Text.Json)
// *************************************************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using UntisLibrary.Api.Entities;


namespace UntisLibrary.Api
{
    /// <summary>
    /// Klasse zum Abfragen von WebUntis.
    /// </summary>
    public class UntisClient : IDisposable
    {
        private User _currentUser;
        private readonly HttpClient _client = new HttpClient();
        private readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = new UntisNamingPolicy() };
        private Lazy<Task<IEnumerable<SchoolClass>>> _classes;
        private Lazy<Task<IEnumerable<Teacher>>> _teachers;
        private Lazy<Task<IEnumerable<Subject>>> _subjects;
        private Lazy<Task<IEnumerable<Room>>> _rooms;
        private Lazy<Task<IEnumerable<Period>>> _periods;
        /// <summary>
        /// Aktiver Schulname in WebUntis (z. B. Spengergasse)
        /// </summary>
        public string School { get; }
        /// <summary>
        /// Anmeldeserver.
        /// </summary>
        public string Server { get; }
        /// <summary>
        /// Basis URL für die Aufrufe der WebUntis JSON-RPC API (https://{Server}/WebUntis/jsonrpc.do)
        /// Wird nur für das Login und Logout verwendet.
        /// </summary>
        public string ApiUrl { get; }
        /// <summary>
        /// Basis URL für die Aufrufe der WebUntis Webseite (https://{Server}/WebUntis/api/public)
        /// </summary>
        public string WebApiUrl { get; }
        /// <summary>
        /// Generierte GUID für die Kommunikation mit der JSON-RPC API
        /// </summary>
        public string ApiClientId { get; }

        /// <summary>
        /// In Untis eingetragene Klassen. Wird bei der ersten Verwendung geladen und bis zum Logout
        /// gespeichert.
        /// </summary>
        public Task<IEnumerable<SchoolClass>> Classes => _classes?.Value ?? Task.FromResult(Enumerable.Empty<SchoolClass>());
        /// <summary>
        /// In Untis eingetragene Lehrer. Wird bei der ersten Verwendung geladen und bis zum Logout
        /// gespeichert.
        /// </summary>
        public Task<IEnumerable<Teacher>> Teachers => _teachers.Value ?? Task.FromResult(Enumerable.Empty<Teacher>());
        /// <summary>
        /// In Untis eingetragene Gegenstände. Wird bei der ersten Verwendung geladen und bis zum Logout
        /// gespeichert.
        /// </summary>
        public Task<IEnumerable<Subject>> Subjects => _subjects.Value ?? Task.FromResult(Enumerable.Empty<Subject>());
        /// <summary>
        /// In Untis eingetragene Räume. Wird bei der ersten Verwendung geladen und bis zum Logout
        /// gespeichert.
        /// </summary>
        public Task<IEnumerable<Room>> Rooms => _rooms.Value ?? Task.FromResult(Enumerable.Empty<Room>());
        /// <summary>
        /// Das In Untis eingetragene Stundenraster. Wird bei der ersten Verwendung geladen 
        /// und bis zum Logout gespeichert.
        /// </summary>
        public Task<IEnumerable<Period>> Periods => _periods.Value ?? Task.FromResult(Enumerable.Empty<Period>());

        /// <summary>
        /// Erstellt die UntisClient Instanz.
        /// </summary>
        /// <param name="server">Anmeldeserver (z. B. neilo.webuntis.com)</param>
        /// <param name="school">Schulname in Webuntis (z. B. Spengergasse)</param>
        public UntisClient(string server, string school)
        {
            Server = server;
            School = school;
            ApiUrl = $"https://{Server}/WebUntis/jsonrpc.do";
            WebApiUrl = $"https://{Server}/WebUntis/api/public";
            ApiClientId = Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Führt ein Login gegen die JSON-RPC API durch und setzt die Requesthandler.
        /// Ist voraussetzung für das Abfragen der Daten.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns>true, wenn erfolgreich, false bei falschen Zugangsdaten.</returns>
        /// <exception cref="UntisException">Netzwerkfehler oder ungültige Antwort.</exception>
        public async Task<bool> TryLoginAsync(string username, string password)
        {
            try
            {
                await LogoutAsync();
                _currentUser = await SendApiRequestAsync<User>("authenticate", new { user = username, password = password, client = ApiClientId });
                _classes = new Lazy<Task<IEnumerable<SchoolClass>>>(() => GetClasses());
                _teachers = new Lazy<Task<IEnumerable<Teacher>>>(() => SendWebRequestAsync<IEnumerable<Teacher>>("timetable/weekly/pageconfig?type=2"));
                _subjects = new Lazy<Task<IEnumerable<Subject>>>(() => SendWebRequestAsync<IEnumerable<Subject>>("timetable/weekly/pageconfig?type=3"));
                _rooms = new Lazy<Task<IEnumerable<Room>>>(() => SendWebRequestAsync<IEnumerable<Room>>("timetable/weekly/pageconfig?type=4"));
                _periods = new Lazy<Task<IEnumerable<Period>>>(() => GetPeriods());
            }
            catch (UntisException e) when (e.Message.Contains ("bad credentials"))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Meldet sich von der JSON-RPC API ab und löscht die gespeicherten Daten in der Instanz.
        /// </summary>
        /// <returns></returns>
        public async Task LogoutAsync()
        {
            if (_currentUser == null) { return; }
            try
            {
                _classes = null;
                _teachers = null;
                _subjects = null;
                _rooms = null;
                _periods = null;
                _currentUser = null;
                await SendApiRequestAsync("logout", null);
            }
            catch { }
        }

        /// <summary>
        /// Liefert alle Schüler einer Klasse.
        /// </summary>
        /// <param name="schoolClass">
        /// Abzufragende Klasse, dessen interne ID übergeben wird. Ist diese null, werden alle 
        /// Schüler geliefert, wenn die Berechtigung ausreichend ist.
        /// </param>
        /// <returns>Task mit der Liste der Schüler.</returns>
        public Task<IEnumerable<Student>> GetStudents(SchoolClass schoolClass = null)
        {
            string filter = "";
            if (schoolClass != null) { filter = $"&filter.klasseOrStudentgroupId=KL{schoolClass.InternalId}"; }

            return SendWebRequestAsync<IEnumerable<Student>>($"timetable/weekly/pageconfig?type=5{filter}");
        }

        /// <summary>
        /// Liefert den Stundenplan einer Klasse einer Woche.
        /// </summary>
        /// <param name="schoolclass">Abzufragende Klasse.</param>
        /// <param name="date">Datum, in dessen Woche der Stundenplan gelesen wird.</param>
        /// <returns></returns>
        /// <exception cref="UntisException">Netzwerkfehler oder ungültige Antwort.</exception>
        public Task<IEnumerable<Lesson>> GetLessons(SchoolClass schoolclass, DateTime date)
        {
            if (schoolclass == null) { return Task.FromResult(Enumerable.Empty<Lesson>()); }
            return GetLessons(1, schoolclass.InternalId, date);
        }
        public Task<IEnumerable<Lesson>> GetLessons(SchoolClass schoolclass) => GetLessons(schoolclass, DateTime.Now);
        /// <summary>
        /// Liefert den Stundenplan eines Lehrers einer Woche.
        /// </summary>
        /// <param name="teacher">Abzufragender Lehrer.</param>
        /// <param name="date">Datum, in dessen Woche der Stundenplan gelesen wird.</param>
        /// <returns></returns>
        /// <exception cref="UntisException">Netzwerkfehler oder ungültige Antwort.</exception>
        public Task<IEnumerable<Lesson>> GetLessons(Teacher teacher, DateTime date)
        {
            if (teacher == null) { return Task.FromResult(Enumerable.Empty<Lesson>()); }
            return GetLessons(2, teacher.InternalId, date);
        }
        public Task<IEnumerable<Lesson>> GetLessons(Teacher teacher) => GetLessons(teacher, DateTime.Now);

        private async Task<IEnumerable<Lesson>> GetLessons(int elementType, int elementId, DateTime date)
        {
            string dateStr = date.ToString("yyyy-MM-dd");
            string url = $"timetable/weekly/data?elementType={elementType}&elementId={elementId}&date={dateStr}";
            List<Lesson> lessons = new List<Lesson>();
            // JSON Daten des Stundenplanes empfangen.
            JsonElement response = await SendWebRequestAsync(url);
            JsonElement parsed;
            try
            {
                // Die einzelnen Unterrichtsstunden behandeln und die Navigation Properties in 
                // Lesson erzeugen. Dabei werden die Collections Lehrer, Räume, ... abgefragt.
                JsonElement periods = response.GetProperty("result").GetProperty("data").GetProperty("elementPeriods").EnumerateObject().First().Value;
                foreach (JsonElement period in periods.EnumerateArray())
                {
                    try
                    {
                        int dateInt = period.GetProperty("date").GetInt32();
                        int beginInt = period.GetProperty("startTime").GetInt32();
                        int endInt = period.GetProperty("endTime").GetInt32();
                        Period lessonPeriod = (await Periods).FirstOrDefault(p => p.StartTime == new TimeSpan(beginInt / 100, beginInt % 100, 0));

                        // Den Status der Stunde in folgender Reihenfolge setzen.
                        LessonState lessonState = LessonState.Other;
                        if (period.GetProperty("is").TryGetProperty("event", out parsed) ? parsed.GetBoolean() : false)
                        { lessonState = LessonState.Event; }
                        else if (period.GetProperty("is").TryGetProperty("substitution", out parsed) ? parsed.GetBoolean() : false)
                        { lessonState = LessonState.Substitution; }
                        else if (period.GetProperty("is").TryGetProperty("shift", out parsed) ? parsed.GetBoolean() : false)
                        { lessonState = LessonState.Shift; }
                        else if (period.GetProperty("is").TryGetProperty("standard", out parsed) ? parsed.GetBoolean() : false)
                        { lessonState = LessonState.Standard; }
                        else if (period.GetProperty("is").TryGetProperty("cancelled", out parsed) ? parsed.GetBoolean() : false)
                        { lessonState = LessonState.Cancelled; }

                        // Die Unterrichtsstunde erzeugen
                        Lesson lesson = new Lesson
                        {
                            StudentGroup = period.TryGetProperty("studentGroup", out parsed) ? parsed.GetString() : "",
                            LessonText = period.TryGetProperty("lessonText", out parsed) ? parsed.GetString() : "",
                            PeriodText = period.TryGetProperty("periodText", out parsed) ? parsed.GetString() : "",

                            Period = lessonPeriod,
                            Begin = new DateTime(dateInt / 10000, dateInt / 100 % 100, dateInt % 100)
                            + new TimeSpan(beginInt / 100, beginInt % 100, 0),
                            End = new DateTime(dateInt / 10000, dateInt / 100 % 100, dateInt % 100)
                            + new TimeSpan(endInt / 100, endInt % 100, 0),

                            State = lessonState
                        };

                        // Eine Stunde kann mehrere Klassen, Lehrer, ... betreffen. Bei einer 
                        // Vertretung wird außerdem die originale ID geliefert.
                        List<LessonResource<SchoolClass>> classes = new List<LessonResource<SchoolClass>>();
                        List<LessonResource<Teacher>> teachers = new List<LessonResource<Teacher>>();
                        List<LessonResource<Subject>> subjects = new List<LessonResource<Subject>>();
                        List<LessonResource<Room>> rooms = new List<LessonResource<Room>>();
                        foreach (JsonElement element in period.GetProperty("elements").EnumerateArray())
                        {
                            int currentId = element.TryGetProperty("id", out parsed) ? parsed.GetInt32() : -1;
                            int originalId = element.TryGetProperty("orgId", out parsed) ? parsed.GetInt32() : -1;

                            // Die Details zu den Ressourcen auslesen. Da es Lazy Properties sind, 
                            // wird nur bei der ersten Verwendung vom Web eingelesen.
                            switch (element.GetProperty("type").GetInt32())
                            {
                                case (int)ResourceType.SchoolClass:
                                    SchoolClass currentClass = (await Classes).FirstOrDefault(c => c.InternalId == currentId);
                                    classes.Add(new LessonResource<SchoolClass>
                                    {
                                        Current = currentClass,
                                        Original = (await Classes)
                                        .FirstOrDefault(c => c.InternalId == originalId) ?? currentClass
                                    }); ;
                                    break;
                                case (int)ResourceType.Teacher:
                                    Teacher currentTeacher = (await Teachers).FirstOrDefault(t => t.InternalId == currentId);

                                    teachers.Add(new LessonResource<Teacher>
                                    {
                                        Current = currentTeacher,
                                        Original = (await Teachers)
                                        .FirstOrDefault(t => t.InternalId == originalId) ?? currentTeacher
                                    });
                                    break;
                                case (int)ResourceType.Subject:
                                    Subject currentSubject = (await Subjects).FirstOrDefault(s => s.InternalId == currentId);

                                    subjects.Add(new LessonResource<Subject>
                                    {
                                        Current = currentSubject,
                                        Original = (await Subjects)
                                        .FirstOrDefault(s => s.InternalId == originalId) ?? currentSubject
                                    });
                                    break;
                                case (int)ResourceType.Room:
                                    Room currentRoom = (await Rooms).FirstOrDefault(r => r.InternalId == currentId);

                                    rooms.Add(new LessonResource<Room>
                                    {
                                        Current = currentRoom,
                                        Original = (await Rooms)
                                        .FirstOrDefault(r => r.InternalId == originalId) ?? currentRoom
                                    });
                                    break;
                                default:
                                    break;
                            }
                        }
                        lesson.Classes = classes;
                        lesson.Teachers = teachers;
                        lesson.Subjects = subjects;
                        lesson.Rooms = rooms;
                        lessons.Add(lesson);
                    }
                    catch { }
                }
            }
            catch (Exception e)
            {
                throw new UntisException("Invalid Timetable data.", e) { Method = url };
            }

            return lessons;
        }

        /// <summary>
        /// Liest eine Liste aller Klassen aus.
        /// </summary>
        /// <returns></returns>
        private async Task<IEnumerable<SchoolClass>> GetClasses()
        {
            IEnumerable<SchoolClass> classes = await SendWebRequestAsync<IEnumerable<SchoolClass>>("timetable/weekly/pageconfig?type=1");
            // Den Klassenvorstand in der Lehrerliste suchen und die Navigation dorthin speichern.
            IEnumerable<Teacher> teachers = await Teachers;
            foreach (SchoolClass schoolClass in classes)
            {
                Teacher classTeacher = teachers.FirstOrDefault(t => t.UniqueName == schoolClass.ClassTeacher?.UniqueName);
                schoolClass.ClassTeacher = classTeacher ?? schoolClass.ClassTeacher;
            }
            return classes;
        }
        /// <summary>
        /// Liest das Stundenraster aus und konvertiert die Zeitangaben in .NET Typen.
        /// </summary>
        /// <returns></returns>
        private async Task<IEnumerable<Period>> GetPeriods()
        {
            JsonElement content = await SendWebRequestAsync("timegrid?schoolyearId=2");

            try
            {
                return from row in content.GetProperty("rows").EnumerateArray()
                       let startTime = row.GetProperty("startTime").GetInt32()
                       let endTime = row.GetProperty("endTime").GetInt32()
                       select new Period
                       {
                           Nr = row.GetProperty("period").GetInt32(),
                           StartTime = new TimeSpan(startTime / 100, startTime % 100, 0),
                           EndTime = new TimeSpan(endTime / 100, endTime % 100, 0),
                       };
            }
            catch (Exception e)
            {
                throw new UntisException("GetPeriods: Invalid Response.", e);
            }
        }

        /// <summary>
        /// Sendet einen GET Request an die URL und deserialisiert die Antwort
        /// mit dem JsonSerializer in den übergebenen Typ.
        /// </summary>
        /// <typeparam name="T">Rückgabetyp für die Deserialisierung</typeparam>
        /// <param name="pageUrl">URL</param>
        /// <returns></returns>
        private async Task<T> SendWebRequestAsync<T>(string pageUrl)
        {
            // Das data Property auslesen.
            JsonElement result = await SendWebRequestAsync(pageUrl);
            try
            {
                // Pageconfig URLs haben die Daten in data/elements/
                if (pageUrl.Contains("pageconfig"))
                {
                    result = result.GetProperty("elements");
                }
                // Timegrid (Stundenplan) URLs haben die Daten in data/rows/
                else if (pageUrl.Contains("timegrid"))
                {
                    result = result.GetProperty("rows");
                }
                return JsonSerializer.Deserialize<T>(result.GetRawText(), _jsonOptions);
            }
            catch (KeyNotFoundException e)
            {
                throw new UntisException("Invalid JSON in Response.", e) { Method = pageUrl };
            }

        }

        /// <summary>
        /// Fordert eine URL mit einem GET Request vom Server an. Cookies werden dabei
        /// aus dem Cookiestore des HTTP Clients mitgesendet, sodass die Authentifizierung
        /// funktioniert.
        /// </summary>
        /// <param name="pageUrl"></param>
        /// <returns>Das data Property des erhaltenen JSON Objektes.</returns>
        private async Task<JsonElement> SendWebRequestAsync(string pageUrl)
        {
            string url = $"{WebApiUrl}/{pageUrl}";
            try
            {
                HttpResponseMessage response = await _client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string content = await response.Content.ReadAsStringAsync();
                try
                {
                    JsonElement elements = JsonDocument.Parse(content).RootElement.GetProperty("data");
                    return elements;
                }
                catch (KeyNotFoundException e)
                {
                    throw new UntisException("Invalid JSON in Response.", e) { Method = pageUrl };
                }
            }
            catch (Exception e)
            {
                throw new UntisException(e.Message, e) { Method = pageUrl };
            }
        }

        /// <summary>
        /// Sendet einen Request an die JSON-RPC API. Dieser hat den Aufbau
        /// {"id":"ID","method":"METHOD NAME","params":{PARAMS},"jsonrpc":"2.0"}
        /// </summary>
        /// <param name="method">API Methodenname in den Parametern des Requests.</param>
        /// <param name="param">Parameterobjekt, welches in params serialisiert wird.</param>
        /// <returns>Antwort der API als String.</returns>
        private async Task<string> SendApiRequestAsync(string method, object param)
        {
            // Zufällige Request ID erzeugen.
            Random rnd = new Random();
            string jsonContent = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = rnd.Next(), method = method, @params = param });
            StringContent request = new StringContent(
                jsonContent,
                Encoding.UTF8,
                "application/json"
            );
            string url = $"{ApiUrl}?school={School}";
            HttpResponseMessage response = await _client.PostAsync(url, request);
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync();
            return content;
        }

        /// <summary>
        /// Sendet einen Request an die JSON-RPC API und deserialisiert das erhaltene
        /// JSON Dokument in den übergebenen Typ.
        /// </summary>
        /// <typeparam name="T">Typ, in den deserialisiert werden soll.</typeparam>
        /// <param name="method">API Methodenname in den Parametern des Requests.</param>
        /// <param name="param">Parameterobjekt, welches in params serialisiert wird.</param>
        /// <returns></returns>
        private async Task<T> SendApiRequestAsync<T>(string method, object param)
        {
            string response = await SendApiRequestAsync(method, param);
            // Passiert ein Fehler, wird ein Property error im JSON Dokument geliefert.
            if (JsonDocument.Parse(response).RootElement.TryGetProperty("error", out JsonElement errorElem))
            {
                throw new UntisException(errorElem.GetProperty("message").GetString())
                {
                    Method = method,
                    ErrorCode = errorElem.GetProperty("code").GetInt32()
                };
            }
            // Ansonsten wird im Property result die Antwort verarbeitet.
            JsonElement result = JsonDocument.Parse(response).RootElement.GetProperty("result");
            return JsonSerializer.Deserialize<T>(result.ToString(), _jsonOptions);
        }

        /// <summary>
        /// Meldet sich von der API ab.
        /// </summary>
        public void Dispose()
        {
            try
            {
                LogoutAsync().Wait();
            }
            catch { }
        }
    }
}
