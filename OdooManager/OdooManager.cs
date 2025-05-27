using static System.Net.Mime.MediaTypeNames;
using System.Net.NetworkInformation;
using OdooRpc.CoreCLR.Client.V8;
using OdooRpc.CoreCLR.Client.V8.Models;
using OdooRpc.CoreCLR.Client.V8.Models.Parameters;
using System.Text;
using Newtonsoft.Json;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
namespace OdooManager
{
    public class OdooManager
    {
        public OdooManager()
        {
        }
        private OdooRpcClient odooClient;
        public async Task Authenticate()
        {
            // Verbindungsparameter definieren
            var connection = new OdooConnectionInfo
            {
                Host = "andagon-holding.cloud",
                IsSSL = true,
                Port = 443,
                Database = "live",
                Username = "s.gogoll@andagon.com",
                //Password = "3cad04cda47116ba5b380d91de92dc2d9bc48453"
                Password = "465eb6b1b80967654bfb77f05357ccfc8a3db974"
            };

            // Odoo-Client erstellen und authentifizieren
            odooClient = new OdooRpcClient(connection);
            await odooClient.Authenticate();
        }
        public string GetOdooVersion()
        {
            // Odoo-Version abrufen
            var version = odooClient.GetOdooVersion().Result;
            string versionString = $"Odoo Version: {version.ServerVersion} (API Version: {version.ProtocolVersion})";
            return versionString;
        }
        public async Task<string> GetModelFields(string modelName)
        {
            //OdooGetModelFieldsParameters parameters = new OdooGetModelFieldsParameters("account.analytic.line");
            OdooGetModelFieldsParameters parameters = new OdooGetModelFieldsParameters(modelName);
            var fields = await odooClient.GetModelFields<AccountAnalyticLine>(parameters);
            StringBuilder sb = new StringBuilder("{");
            foreach (var field in fields)
            {
                sb.Append($"\"{field.Key}\":");
                sb.Append("null,");
            }
            ;
            sb.Append("}");
            string json = sb.ToString();
            return json;
        }
        public async Task<ProjectProject> GetProjectAsync(IEnumerable<long> ids)
        {
            long id = ids.FirstOrDefault();
            OdooGetParameters getParams = new OdooGetParameters("project.project",ids);
            OdooDomainFilter domainFilter = new OdooDomainFilter();
            //domainFilter.Filter("id", "=", ids.FirstOrDefault());
            var searchParams = new OdooSearchParameters("project.project", domainFilter);
            var fieldParams = new OdooFieldParameters(new List<string> { "id", "name", "task_ids", "active" });
            var projects = await odooClient.GetAll<Object>("project.project", fieldParams);        // Mitarbeiterdaten definieren
            string json = projects.ToString();
            List<ProjectProject> Projects = JsonConvert.DeserializeObject<List<ProjectProject>>(json);
            //var result = await odooClient.GetAll<Object>("hr.empolyee", new OdooFieldParameters());
            //string json = result.ToString();
            var p = Projects.Where(p => p.id == id).FirstOrDefault();
            return p;
        }
        public async Task<List<SimpleResult>> GetEmployeesAsync(bool active = true)
        {
            var domainFilter = new OdooDomainFilter();
            domainFilter.Filter("active", "=", active); 

            // Suchparameter für das Modell "hr.employee" erstellen
            var searchParams = new OdooSearchParameters("hr.employee", domainFilter);

            // Einen bestimmten Mitarbeiter abrufen
            SimpleResult[] Employees = await odooClient.Get<SimpleResult[]>(searchParams);
            return Employees.ToList();

        }
        public async Task<string> GenerateJsonExampleForModel(string modelName)
        {
            // Parameter für GetModelFields erstellen
            var fieldParams = new OdooGetModelFieldsParameters(modelName);

            // Alle Felder des Modells abrufen
            var fields = await odooClient.GetModelFields<Dictionary<string, object>>(fieldParams);
            // Dictionary für Beispieldaten erstellen
            var exampleData = new Dictionary<string, object>();

            // Für jedes Feld Beispieldaten generieren
            foreach (var field in fields)
            {
                string fieldName = field.Key;
                var fieldInfo = field.Value;
                string fieldType = fieldInfo["type"]?.ToString() ?? "unknown";

                // Beispieldaten basierend auf dem Datentyp
                switch (fieldType.ToLower())
                {
                    case "char":
                        exampleData[fieldName] = $"Beispiel {fieldName}";
                        break;
                    case "text":
                        exampleData[fieldName] = $"Dies ist ein längerer Beispieltext für {fieldName}.";
                        break;
                    case "integer":
                        exampleData[fieldName] = 42;
                        break;
                    case "float":
                        exampleData[fieldName] = 12.34;
                        break;
                    case "boolean":
                        exampleData[fieldName] = true;
                        break;
                    case "date":
                        exampleData[fieldName] = "2025-03-21"; // ISO-Format
                        break;
                    case "datetime":
                        exampleData[fieldName] = "2025-03-21 14:30:00"; // ISO-Format mit Zeit
                        break;
                    case "many2one":
                        exampleData[fieldName] = new object[] { 1, $"Beispiel {fieldName}" }; // [ID, Name]
                        break;
                    case "one2many":
                    case "many2many":
                        exampleData[fieldName] = new List<object> { 1, 2, 3 }; // Liste von IDs
                        break;
                    case "selection":
                        // Annahme: Erster Wert aus den Auswahlmöglichkeiten (falls verfügbar)
                        if (fieldInfo.ContainsKey("selection") && fieldInfo["selection"] is List<object> selection)
                        {
                            var firstOption = (selection[0] as object[])?[0];
                            exampleData[fieldName] = firstOption ?? "option1";
                        }
                        else
                        {
                            exampleData[fieldName] = "option1";
                        }
                        break;
                    default:
                        exampleData[fieldName] = null; // Unbekannter Typ, null als Platzhalter
                        break;
                }
            }

            // JSON-String generieren
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true // Für lesbare Formatierung
            };
            var result = System.Text.Json.JsonSerializer.Serialize(exampleData, jsonOptions);
            return result;
        }
        public async Task<HrEmployee?>GetEmployeeByEmailAsync(string workemail)
        {
            var domainFilter = new OdooDomainFilter();
            domainFilter.Filter("work_email", "=", workemail); // Suche nach der E-Mail-Adresse

            // Suchparameter für das Modell "hr.employee" erstellen
            var searchParams = new OdooSearchParameters("hr.employee", domainFilter);

            // Einen bestimmten Mitarbeiter abrufen
            HrEmployee[] Employees = await odooClient.Get<HrEmployee[]>(searchParams);
            HrEmployee e;
            if (Employees.Length > 0)
            {
                e = Employees[0];
            }
            else
            {
                return null;
            }
            return e;
        }
        public async Task<HrEmployee?> GetEmployeeByIdAsync(long id)
        {
            var domainFilter = new OdooDomainFilter();
            domainFilter.Filter("id", "=", id); // Suche nach der E-Mail-Adresse

            // Suchparameter für das Modell "hr.employee" erstellen
            var searchParams = new OdooSearchParameters("hr.employee", domainFilter);

            // Einen bestimmten Mitarbeiter abrufen
            HrEmployee[] Employees = await odooClient.Get<HrEmployee[]>(searchParams);
            HrEmployee e;
            if (Employees.Length > 0)
            {
                e = Employees[0];
            }
            else
            {
                return null;
            }
            return e;
        }
        public async Task<ProjectTask> GetProjectTaskAsync(long id)
        {
            OdooDomainFilter domainFilter = new OdooDomainFilter();
            //var fieldParams = new OdooFieldParameters(new List<string> { "id", "name", "description", "priority", "stage_id", "tag_ids", "state", "create_date", "write_date", "date_end", "date_assign", "date_deadline", "date_last_stage_update", "project_id", "display_in_project" });
            var fieldParams = new OdooFieldParameters();
            var result = await odooClient.GetAll<Object>("project.task", fieldParams);
            string json = result.ToString();
            List<ProjectTask> tasks = JsonConvert.DeserializeObject<List<ProjectTask>>(json);
            ProjectTask t;
            if (tasks != null && tasks.Count > 0)
            {
                t = tasks.Where(t => t.id == id).FirstOrDefault();
            }
            else
            {
                t = new ProjectTask();
            }
            return t;
        }
        public async Task<bool> SaveTimeBooking(long taskId, long projectId, double amount, string description, long employeeId)
        {
            try
            {
                var timesheetData = new Dictionary<string, object>
                {
                    { "task_id", taskId },                  // ID des ProjectTask
                    { "project_id", projectId },                // ID des Projekts (muss korrekt sein)
                    { "date", DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd") }, // Datum der Buchung
                    { "unit_amount", amount },             // Gebuchte Zeit in Stunden (z. B. 2,5 Stunden)
                    { "name", description },    // Beschreibung der Zeitbuchung
                    { "employee_id", employeeId }   // ID des Mitarbeiters, falls nicht automatisch gesetzt
                };
                var timesheetId = await odooClient.Create("account.analytic.line", timesheetData);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        private async Task<string> ExcelToJson(string excelFilePath)
        {
            using SpreadsheetDocument document = SpreadsheetDocument.Open(excelFilePath, false);
            WorkbookPart workbookPart = document.WorkbookPart;
            WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
            SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

            List<Dictionary<string, object>> data = new List<Dictionary<string, object>>();
            List<string> headers = new List<string>();

            // Überschriften erfassen
            Row headerRow = sheetData.Elements<Row>().First();
            foreach (Cell cell in headerRow.Elements<Cell>())
            {
                headers.Add(GetCellValue(document, cell));
            }

            // Datenzeilen erfassen
            foreach (Row row in sheetData.Elements<Row>().Skip(1))
            {
                Dictionary<string, object> rowData = new Dictionary<string, object>();
                int colIndex = 0;
                foreach (Cell cell in row.Elements<Cell>())
                {
                    rowData[headers[colIndex++]] = GetCellValue(document, cell);
                }
                data.Add(rowData);
            }

            var result = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            return result;
        }
        private static string GetCellValue(SpreadsheetDocument document, Cell cell)
        {
            string value = cell.InnerText;
            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                return document.WorkbookPart.SharedStringTablePart.SharedStringTable.ChildElements[int.Parse(value)].InnerText;
            }
            return value;
        }
        public async Task ImportEmployees()
        {
            // Excel-Datei mit Mitarbeiterdaten
            string excelFilePath = "C:\\Users\\sgogo\\Downloads\\HRWORKS_user.xlsx";
            // Excel-Datei in JSON konvertieren
            string json = await ExcelToJson(excelFilePath);
            // JSON in Liste von Person-Objekten konvertieren
            List<Person> persons = JsonConvert.DeserializeObject<List<Person>>(json);
            // Für jeden Mitarbeiter ein HrEmployee-Objekt erstellen
            var activecount = persons.Count(persons => persons.Austrittsdatum == null || persons.Austrittsdatum == "" && persons.EMail != null);
            foreach (var person in persons.Where(p=>p.EMail != null))
            {
                // Mitarbeiter in Odoo anlegen
                var e = await GetEmployeeByEmailAsync(person.EMail);
                if(e == null)
                {
                    await CreateHrEmployee(person);
                }
                else
                {
                    await UpdateHrEmployee(e.id, person);
                }
                Console.WriteLine($"Mitarbeiter {person.Name} {person.Vorname} importiert.");
            }
            return;
        }
        public async Task<bool> DeleteEmployee(int employeeId)
        {
            try
            {
                await odooClient.Delete("hr.employee", employeeId);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }
        public async Task<bool> DeleteProjectTask(long taskId)
        {
            try
            {
                // ProjektTask in Odoo löschen
                await odooClient.Delete("project.task", taskId);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        private async Task<long> CreateHrEmployee(Person person)
        {
            try
            {
                // Dictionary für die Mitarbeiterdaten erstellen
                var employeeData = new Dictionary<string, object>();

                // Alle Eigenschaften der Person-Klasse zu Odoo-Feldern mappen
                if (!string.IsNullOrEmpty(person.Name))
                {
                    // Kombinieren von Vorname und Name, falls Vorname existiert
                    employeeData["name"] = string.IsNullOrEmpty(person.Vorname)
                        ? person.Name
                        : $"{person.Vorname} {person.Name}";
                }
                //if (!string.IsNullOrEmpty(person.Hausnummer))
                //{
                //    person.Strasse += " " + person.Hausnummer;
                //}
                //if (!string.IsNullOrEmpty(person.Strasse)) employeeData["private_street"] = person.Strasse;
                //if (!string.IsNullOrEmpty(person.Plz)) employeeData["private_zip"] = person.Plz;
                //if (!string.IsNullOrEmpty(person.Ort)) employeeData["private_city"] = person.Ort;
                //if (!string.IsNullOrEmpty(person.AdresseLand)) employeeData["private_country_id"] = person.AdresseLandId; // Erwartet ID oder Name für Suche
                //if (!string.IsNullOrEmpty(person.AdresseBundesland)) employeeData["private_state_id"] = 1396; // Erwartet ID oder Name
                //if (!string.IsNullOrEmpty(person.Geburtsland)) employeeData["country_of_birth"] = person.GeburtslandId; // Erwartet ID oder Name
                //if (!string.IsNullOrEmpty(person.Geburtsland)) employeeData["country_id"] = person.GeburtslandId; // Erwartet ID oder Name
                if (!string.IsNullOrEmpty(person.EMail)) employeeData["work_email"] = person.EMail;
                //if (!string.IsNullOrEmpty(person.PrivateEMail)) employeeData["private_email"] = person.PrivateEMail;
                //if (!string.IsNullOrEmpty(person.Telefon)) employeeData["work_phone"] = person.Telefon;
                //if (!string.IsNullOrEmpty(person.HandynummerPrivat)) employeeData["private_phone"] = person.HandynummerPrivat;
                //if (!string.IsNullOrEmpty(person.HandynummerGeschaeftlich)) employeeData["work_phone"] = person.HandynummerGeschaeftlich;
                //if (!string.IsNullOrEmpty(person.GebaeudeRaum)) employeeData["work_location_id"] = 2;
                //if (!string.IsNullOrEmpty(person.Geburtsname)) employeeData["x_studio_geburtsname"] = person.Geburtsname;
                //if (!string.IsNullOrEmpty(person.Geburtsort)) employeeData["place_of_birth"] = person.Geburtsort;
                //if (!string.IsNullOrEmpty(person.BIC)) employeeData["x_studio_bic"] = person.BIC;
                //if (!string.IsNullOrEmpty(person.IBAN)) employeeData["x_studio_iban"] = person.IBAN;
                //if (!string.IsNullOrEmpty(person.Sozialversicherungsnummer)) employeeData["ssnid"] = person.Sozialversicherungsnummer;
                //if (!string.IsNullOrEmpty(person.Steueridentifikationsnummer)) employeeData["identification_id"] = person.Steueridentifikationsnummer;
                //if (!string.IsNullOrEmpty(person.Geburtsdatum)) employeeData["birthday"] = ConvertToISO8601(person.Geburtsdatum);
                //if (!string.IsNullOrEmpty(person.Eintrittsdatum)) employeeData["x_studio_eintrittsdatum"] = ConvertToISO8601(person.Eintrittsdatum);
                //if (!string.IsNullOrEmpty(person.Austrittsdatum))
                //{
                //    //employeeData["x_studio_austrittsdatum"] = ConvertToISO8601(person.Austrittsdatum);
                //    employeeData["active"] = false;
                //}
                //if (!string.IsNullOrEmpty(person.Position)) employeeData["job_title"] = person.Position;
                //if (!string.IsNullOrEmpty(person.NotfallkontaktBeziehung))
                //{
                //    person.NotfallkontaktName += " (" + person.NotfallkontaktBeziehung + ")";
                //    employeeData["x_studio_notfallkontakt_beziehung"] = person.NotfallkontaktBeziehung;
                //}
                //if (!string.IsNullOrEmpty(person.NotfallkontaktName)) employeeData["emergency_contact"] = person.NotfallkontaktName;
                //if (!string.IsNullOrEmpty(person.NotfallkontaktTelefon)) employeeData["emergency_phone"] = person.NotfallkontaktTelefon;
                //if (!string.IsNullOrEmpty(person.Geschlecht)) employeeData["gender"] = person.Geschlecht.ToLower() switch
                //{
                //    "männlich" => "male",
                //    "weiblich" => "female",
                //    "divers" => "other",
                //    _ => "other" // Standardwert für unbekannte Eingaben
                //};
                // Festgelegte company_id hinzufügen (andagon people GmbH, ID 4)
                employeeData["company_id"] = 4;
                // Mitarbeiter in Odoo anlegen
                var newId = await odooClient.Create("hr.employee", employeeData);
                await UpdateHrEmployee(newId, person);
                return newId; // Rückgabe der neuen Mitarbeiter-ID
            }
            catch (Exception ex)
            {
                return -1;
            }
        }
        async Task UpdateHrEmployee(long employeeId, Person person)
        {
            try
            {
                // Dictionary für die zu aktualisierenden Mitarbeiterdaten erstellen
                var employeeData = new Dictionary<string, object>();

                // Alle Eigenschaften der Person-Klasse zu Odoo-Feldern mappen
                if (!string.IsNullOrEmpty(person.Name))
                {
                    // Kombinieren von Vorname und Name, falls Vorname existiert
                    employeeData["name"] = string.IsNullOrEmpty(person.Vorname)
                        ? person.Name
                        : $"{person.Vorname} {person.Name}";
                }
                if (!string.IsNullOrEmpty(person.Hausnummer))
                {
                    person.Strasse += " " + person.Hausnummer;
                }
                if (!string.IsNullOrEmpty(person.Strasse)) employeeData["private_street"] = person.Strasse;
                if (!string.IsNullOrEmpty(person.Plz)) employeeData["private_zip"] = person.Plz;
                if (!string.IsNullOrEmpty(person.Ort)) employeeData["private_city"] = person.Ort;
                if (!string.IsNullOrEmpty(person.AdresseLand)) employeeData["private_country_id"] = person.AdresseLandId; // Erwartet ID oder Name für Suche
                if (!string.IsNullOrEmpty(person.AdresseBundesland)) employeeData["private_state_id"] = 1396; // Erwartet ID oder Name
                if (!string.IsNullOrEmpty(person.Geburtsland)) employeeData["country_of_birth"] = person.GeburtslandId; // Erwartet ID oder Name
                if (!string.IsNullOrEmpty(person.Geburtsland)) employeeData["country_id"] = person.GeburtslandId; // Erwartet ID oder Name
                if (!string.IsNullOrEmpty(person.EMail)) employeeData["work_email"] = person.EMail;
                if (!string.IsNullOrEmpty(person.PrivateEMail)) employeeData["private_email"] = person.PrivateEMail;
                if (!string.IsNullOrEmpty(person.Telefon)) employeeData["work_phone"] = person.Telefon;
                if (!string.IsNullOrEmpty(person.HandynummerPrivat)) employeeData["private_phone"] = person.HandynummerPrivat;
                if (!string.IsNullOrEmpty(person.HandynummerGeschaeftlich)) employeeData["work_phone"] = person.HandynummerGeschaeftlich;
                if (!string.IsNullOrEmpty(person.GebaeudeRaum)) employeeData["work_location_id"] = 2;
                if (!string.IsNullOrEmpty(person.Geburtsname)) employeeData["x_studio_geburtsname"] = person.Geburtsname;
                if (!string.IsNullOrEmpty(person.Geburtsort)) employeeData["place_of_birth"] = person.Geburtsort;
                if (!string.IsNullOrEmpty(person.BIC)) employeeData["x_studio_bic"] = person.BIC;
                if (!string.IsNullOrEmpty(person.IBAN)) employeeData["x_studio_iban"] = person.IBAN;
                if (!string.IsNullOrEmpty(person.Sozialversicherungsnummer)) employeeData["ssnid"] = person.Sozialversicherungsnummer;
                if (!string.IsNullOrEmpty(person.Steueridentifikationsnummer)) employeeData["identification_id"] = person.Steueridentifikationsnummer;
                if (!string.IsNullOrEmpty(person.Geburtsdatum)) employeeData["birthday"] = ConvertToISO8601(person.Geburtsdatum);
                if (!string.IsNullOrEmpty(person.Eintrittsdatum)) employeeData["x_studio_eintrittsdatum"] = ConvertToISO8601(person.Eintrittsdatum);
                if (!string.IsNullOrEmpty(person.Austrittsdatum))
                {
                    employeeData["x_studio_austrittsdatum"] = ConvertToISO8601(person.Austrittsdatum);
                    employeeData["active"] = false;
                }
                if (!string.IsNullOrEmpty(person.Position)) employeeData["job_title"] = person.Position;
                if (!string.IsNullOrEmpty(person.NotfallkontaktBeziehung))
                {
                    person.NotfallkontaktName += " (" + person.NotfallkontaktBeziehung + ")";
                    employeeData["x_studio_notfallkontakt_beziehung"] = person.NotfallkontaktBeziehung;
                }
                if (!string.IsNullOrEmpty(person.NotfallkontaktName)) employeeData["emergency_contact"] = person.NotfallkontaktName;
                if (!string.IsNullOrEmpty(person.NotfallkontaktTelefon)) employeeData["emergency_phone"] = person.NotfallkontaktTelefon;
                if (!string.IsNullOrEmpty(person.Geschlecht)) employeeData["gender"] = person.Geschlecht.ToLower() switch
                {
                    "männlich" => "male",
                    "weiblich" => "female",
                    "divers" => "other",
                    _ => "other" // Standardwert für unbekannte Eingaben
                };

                // Mitarbeiter in Odoo aktualisieren
                await odooClient.Update("hr.employee", employeeId, employeeData);
            }
            catch (Exception ex)
            {
                return;
            }
        }
        public async Task<List<ResCountry>> GetAllCountries()
        {
            // Felder definieren, die abgerufen werden sollen
            var fieldParams = new OdooFieldParameters(new List<string>
            {
                "id",
                "name"
            });

            // Alle Länder abrufen
            var countries = await odooClient.GetAll<ResCountry[]>("res.country", fieldParams);
            StringBuilder sb = new StringBuilder();
            foreach (var country in countries)
            {
                sb.AppendLine($"{country.id}: {country.name}\n");
            }
            string s = sb.ToString();
            return countries.ToList() ?? new List<ResCountry>(); // Rückgabe der Liste, leer wenn null
        }
        public async Task<List<ResCountryState>> GetAllCountryStates()
        {
            var searchParams = new OdooSearchParameters("res.country.state", new OdooDomainFilter().Filter("country_id", "=", 57));

            // IDs der deutschen Bundesländer abrufen
            var states = await odooClient.Get<ResCountryState[]>(searchParams);

            return states.ToList() ?? new List<ResCountryState>(); // Rückgabe der Liste, leer wenn null
        }
        private string ConvertToISO8601(string sDate)
        {
            DateTime date = DateTime.Parse(sDate);
            return date.ToString("yyyy-MM-dd");
        }
        public async Task<List<SimpleResult>> GetProjectTasksByEmail(string email)
        {
            var product = await GetProductByEmailAsync(email);
            if (product == null)
            {
                return new List<SimpleResult>();
            }
            var domainFilter = new OdooDomainFilter();
            domainFilter.Filter("x_studio_produkt", "=", product.id); // Suche nach der E-Mail-Adresse
            // Suchparameter für das Modell "res.partner" erstellen
            var searchParams = new OdooSearchParameters("project.task", domainFilter);

            SimpleResult[] result = await odooClient.Get<SimpleResult[]>(searchParams);
            var tasks = result.ToList();
            return tasks;
        }
        public async Task<SimpleProjectTask> GetProjectTaskById(long id)
        {
            var domainFilter = new OdooDomainFilter();
            domainFilter.Filter("id", "=", id);
            var searchParams = new OdooSearchParameters("project.task", domainFilter);

            var result = await odooClient.Get<Object>(searchParams);
            string json = result.ToString();
            var tasks = JsonConvert.DeserializeObject<SimpleProjectTask[]>(json);
            //if (result == null || result.Length == 0)
            //{
            //    return null;
            //}
            var task = tasks.FirstOrDefault();
            return task;
        }
        public async Task<ProductProduct?> GetProductByEmailAsync(string email)
        {
            var domainFilter = new OdooDomainFilter();
            domainFilter.Filter("x_studio_email", "=", email); // Suche nach der E-Mail-Adresse
            // Suchparameter für das Modell "res.partner" erstellen
            var searchParams = new OdooSearchParameters("product.product", domainFilter);
            // Einen bestimmten Partner abrufen
            ProductProduct[] products = await odooClient.Get<ProductProduct[]>(searchParams);
            if (products.Length == 0)
            {
                return null;
            }
            var p = products.FirstOrDefault();
            return p;
        }
        public async Task<bool> SaveAttendnace(long employeeId,DateTime day, DateTime checkIn, DateTime checkOut, long attendenceId = 0)
        {
            try
            {
                Dictionary<string, object> updateData;
                if(attendenceId > 0)
                {
                    HrAttendance attendance = await GetAttendancesById(attendenceId);
                    if (attendance == null)
                    {
                        return false;
                    }
                    updateData = new Dictionary<string, object>
                    {
                        { "check_in", checkIn.ToString("yyyy-MM-dd HH:mm:ss") }, // Datum und Uhrzeit des Check-Ins
                        { "check_out", checkOut.ToString("yyyy-MM-dd HH:mm:ss") } // Datum und Uhrzeit des Check-Outs
                    };
                    await odooClient.Update("hr.attendance", attendance.id, updateData);
                    return true;
                }
                checkIn = day.Date.AddHours(checkIn.Hour).AddMinutes(checkIn.Minute);
                checkOut = day.Date.AddHours(checkOut.Hour).AddMinutes(checkOut.Minute);
                List<HrAttendance> result = await GetAttendancesByEmployeeIdAndDate(employeeId, day);
                foreach (var r in result)
                {
                    HrAttendance attendance = await GetAttendancesById(r.id);
                    DateTime cIn = DateTime.Parse(r.check_in);
                    DateTime cOut = r.check_out == "false" ? DateTime.MinValue : DateTime.Parse(r.check_out);
                    if (checkIn >= cIn && r.check_out == "false")
                    {
                        updateData = new Dictionary<string, object>
                        {
                            { "check_out", checkOut.ToString("yyyy-MM-dd HH:mm:ss") } // Datum und Uhrzeit des Check-Outs
                        };
                        await odooClient.Update("hr.attendance", r.id, updateData);
                        return true;
                    }
                    if (checkIn < cIn && checkOut > cIn)
                    {
                        updateData = new Dictionary<string, object>
                        {
                            { "check_in", checkIn.ToString("yyyy-MM-dd HH:mm:ss") }, // Datum und Uhrzeit des Check-Ins
                            { "check_out", checkOut.ToString("yyyy-MM-dd HH:mm:ss") } // Datum und Uhrzeit des Check-Outs
                        };
                        await odooClient.Update("hr.attendance", r.id, updateData);
                        return true;
                    }
                }
                if (checkOut <= checkIn) return false;
                // Dictionary für die Anwesenheitsdaten erstellen
                var attendanceData = new Dictionary<string, object>
                {
                    { "employee_id", employeeId },                  // ID des Mitarbeiters
                    { "check_in", checkIn.ToString("yyyy-MM-dd HH:mm:ss") }, // Datum und Uhrzeit des Check-Ins
                    { "check_out", checkOut.ToString("yyyy-MM-dd HH:mm:ss") } // Datum und Uhrzeit des Check-Outs
                };
                var attendanceId = await odooClient.Create("hr.attendance", attendanceData);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<List<HrAttendance>> GetAttendancesByEmployeeIdAndDate(long employeeId, DateTime date)
        {
            // Suchparameter für das Modell "hr.attendance" erstellen
            var searchParams = new OdooSearchParameters("hr.attendance", new OdooDomainFilter()
                .Filter("employee_id", "=", employeeId)
                .Filter("check_in", ">=", date.Date.ToString("yyyy-MM-dd HH:mm:ss"))
                .Filter("check_in", "<", date.Date.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss"))
            );
            // Anwesenheitsdaten abrufen
            HrAttendance[] attendances = await odooClient.Get<HrAttendance[]>(searchParams);
            if(attendances.Length == 0)
            {
                return new List<HrAttendance>();
            }
            return attendances.ToList();
        }
        public async Task<HrAttendance> GetAttendancesById(long attendenceId)
        {
            // Suchparameter für das Modell "hr.attendance" erstellen
            var searchParams = new OdooSearchParameters("hr.attendance", new OdooDomainFilter()
                .Filter("id", "=", attendenceId)
            );
            // Anwesenheitsdaten abrufen
            HrAttendance[] attendances = await odooClient.Get<HrAttendance[]>(searchParams);
            if (attendances.Length == 0)
            {
                return new HrAttendance();
            }
            return attendances.ToList().FirstOrDefault();
        }
        public async Task<long> GetEmployeeIdByEmail(string email)
        {
            var searchParams = new OdooSearchParameters("hr.employee", new OdooDomainFilter().Filter("work_email", "=", email));
            var fieldParams = new OdooFieldParameters(new List<string> { "id" });


            // Einen bestimmten Mitarbeiter abrufen
            SimpleResult[] EmployeesIds = await odooClient.Get<SimpleResult[]>(searchParams,fieldParams);
            if (EmployeesIds.Length == 0)
            {
                return 0;
            }
            var e = EmployeesIds.FirstOrDefault();
            return e.id;

        }
        public async Task ReportSickLeave(long employeeId, DateTime startDate, int numberOfDays)
        {
            // Datum im Format "YYYY-MM-DD" für Odoo
            string dateFrom = startDate.ToString("yyyy-MM-dd");
            string dateTo = startDate.AddDays(numberOfDays - 1).ToString("yyyy-MM-dd");

            // Abwesenheitsart "Krankheit" finden (ID der Abwesenheitsart)
            var searchParams = new OdooSearchParameters("hr.leave.type");
            var leaveTypes = await odooClient.Get<SimpleResult[]>(searchParams);
            long leaveTypeId = leaveTypes.FirstOrDefault(lt => lt.name == "Krankheit")?.id ?? 0;
            // Daten für die Krankmeldung
            var leaveData = new Dictionary<string, object>
            {
                { "employee_id", employeeId },               // Mitarbeiter-ID
                { "holiday_status_id", leaveTypeId },       // Abwesenheitsart (Krankheit)
                { "date_from", $"{dateFrom} 08:00:00" },    // Startdatum (z. B. 8:00 Uhr)
                { "date_to", $"{dateTo} 17:00:00" },        // Enddatum (z. B. 17:00 Uhr)
                { "number_of_days", numberOfDays },         // Anzahl der Tage
                { "name", "Krankmeldung" },                 // Beschreibung
                { "state", "validate" }                     // Direkt validieren (optional, abhängig von Workflow)
            };

            // Krankmeldung in Odoo erstellen
            var newLeaveId = await odooClient.Create("hr.leave", leaveData);
            Console.WriteLine($"Krankmeldung mit ID {newLeaveId} erstellt.");
        }
        public async Task<SimpleResult> GetProjectByTaskId(long taskId)
        {
            // 1. Task holen
            var task = await GetProjectTaskById(taskId);
            if (task == null || task.project_id == null || task.project_id.Length == 0)
            {
                // Kein Projekt gefunden oder ungültige Task
                return new SimpleResult();
            }

            // 2. Project-ID auslesen (Odoo: [id, name])
            var projectIdObj = task.project_id[0];
            if (projectIdObj == null)
                return new SimpleResult();
            long projectId = 0;
            if (!long.TryParse(projectIdObj.ToString(), out projectId))
                return new SimpleResult();

            // 3. Projektdaten holen
            var domainFilter = new OdooDomainFilter();
            domainFilter.Filter("id", "=", projectId);
            var searchParams = new OdooSearchParameters("project.project", domainFilter);

            // "id" und "name" reichen hier (SimpleResult)
            var fieldParams = new OdooFieldParameters(new List<string> { "id", "name" });

            SimpleResult[] projects = await odooClient.Get<SimpleResult[]>(searchParams, fieldParams);
            return projects.ToList().FirstOrDefault();
        }
        public async Task<List<TimesheetResult>> GetTimesheetsByTaskId(long taskId, long employeeId)
        {
            // Domain: Nur Timesheets mit passender task_id
            var domainFilter = new OdooDomainFilter();
            domainFilter.Filter("task_id", "=", taskId);
            domainFilter.Filter("employee_id", "=", employeeId);

            // Felder definieren: id, date, name (Beschreibung), unit_amount (Stunden)
            var fieldParams = new OdooFieldParameters(new List<string> { "id", "date", "name", "unit_amount" });
            //await GetModelFields("account.analytic.line");

            var searchParams = new OdooSearchParameters("account.analytic.line", domainFilter);

            // Odoo liefert als Object[], daraus serialisieren wir die Liste
            var result = await odooClient.Get<Object[]>(searchParams, fieldParams);

            if (result == null || result.Length == 0)
                return new List<TimesheetResult>();

            // Serialisieren & mappen auf DTO
            var json = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            // Mapping auf TimesheetResult
            var timesheets = Newtonsoft.Json.JsonConvert.DeserializeObject<List<TimesheetResult>>(json);

            // Optional: Feldnamen mappen, falls description leer (falls Odoo-Feld manchmal fehlt)
            if (timesheets != null)
            {
                foreach (var t in timesheets)
                {
                    if (string.IsNullOrEmpty(t.description))
                        t.description = "";
                }
            }

            return timesheets ?? new List<TimesheetResult>();
        }
        public async Task<List<TimesheetResult>> GetTimesheetsByMonth(DateTime month, long employeeId)
        {
            // Ersten und letzten Tag des Monats berechnen
            var firstDay = new DateTime(month.Year, month.Month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);

            // Odoo erwartet Datum als String im Format "yyyy-MM-dd"
            string dateFrom = firstDay.ToString("yyyy-MM-dd");
            string dateTo = lastDay.ToString("yyyy-MM-dd");

            // Domain-Filter auf employee_id und Datumsbereich
            var domainFilter = new OdooDomainFilter();
            domainFilter.Filter("employee_id", "=", employeeId);
            domainFilter.Filter("date", ">=", dateFrom);
            domainFilter.Filter("date", "<=", dateTo);

            // Felder: id, date, name (description), unit_amount (amount)
            var fieldParams = new OdooFieldParameters(new List<string> { "id", "date", "name", "unit_amount" });

            var searchParams = new OdooSearchParameters("account.analytic.line");

            var result = await odooClient.Get<Object[]>(searchParams);

            if (result == null || result.Length == 0)
                return new List<TimesheetResult>();

            // Serialisieren und auf DTO mappen
            var json = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var timesheets = Newtonsoft.Json.JsonConvert.DeserializeObject<List<TimesheetResult>>(json);

            // Optional: leere Descriptions abfangen
            if (timesheets != null)
            {
                foreach (var t in timesheets)
                {
                    if (string.IsNullOrEmpty(t.description))
                        t.description = "";
                }
            }

            return timesheets ?? new List<TimesheetResult>();
        }

    }
}
public class SimpleResult
{
    public long id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
}
public class SimpleProjectTask : SimpleResult
{
    public object[] project_id { get; set; }
}
public class Person
{
    public string Name { get; set; }
    public string Vorname { get; set; }
    public string Strasse { get; set; }
    public string Hausnummer { get; set; }
    public string Plz { get; set; }
    public string Ort { get; set; }
    public string AdresseLand { get; set; }
    public long AdresseLandId { get; set; }
    public string AdresseBundesland { get; set; }
    public string Geburtsland { get; set; }
    public long GeburtslandId { get; set; }
    public string EMail { get; set; }
    public string PrivateEMail { get; set; }
    public string Telefon { get; set; }
    public string Anrede { get; set; }
    public string HandynummerPrivat { get; set; }
    public string HandynummerGeschaeftlich { get; set; }
    public string GebaeudeRaum { get; set; }
    public string Geburtsname { get; set; }
    public string Geburtsort { get; set; }
    public string BIC { get; set; }
    public string IBAN { get; set; }
    public string Sozialversicherungsnummer { get; set; }
    public string Steueridentifikationsnummer { get; set; }
    public string Geburtsdatum { get; set; }
    public string Eintrittsdatum { get; set; }
    public string Austrittsdatum { get; set; }
    public string EnddatumProbezeit { get; set; }
    public string Staatsangehoerigkeit { get; set; }
    public long StaatsangehoerigkeitId { get; set; }
    public string ZweiteStaatsangehoerigkeit { get; set; }
    public string Position { get; set; }
    public string NotfallkontaktName { get; set; }
    public string NotfallkontaktTelefon { get; set; }
    public string NotfallkontaktBeziehung { get; set; }
    public string Geschlecht { get; set; }
}
public class TimesheetResult
{
    public long id { get; set; }
    public string date { get; set; }
    public string description { get; set; }
    public double unit_amount { get; set; }
    public long task_id { get; set; } // ID des Tasks
    public string Hours
    {
        get
        {
            var hours = unit_amount / 60;
            var minutes = unit_amount % 60;
            return $"{hours:D2}:{minutes:D2}";
        }
        set
        {
            var parts = value.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out var hours) && int.TryParse(parts[1], out var minutes))
            {
                unit_amount = hours * 60 + minutes;
            }
            else
            {
                throw new FormatException("Invalid time format. Expected format is HH:mm.");
            }
        }
    }
}