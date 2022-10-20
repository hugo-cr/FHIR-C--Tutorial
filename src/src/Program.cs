using System;
using System.Collections.Generic;
using Hl7.Fhir.Model; //Contiene los tipos de datos de los recursos FHIR.
using Hl7.Fhir.Rest; //Para llamadas HTTP y cliente FHIR.
using Hl7.Fhir.Serialization;

namespace fhir_cs_tutorial
{
    public static class Program
    {
        

        private static readonly Dictionary<string,string> _fhirServers = new Dictionary<string, string>()
        {
            {"PublicVonk", "http://vonk.fire.ly"},
            {"PublicHAPI", "http://hapi.fhir.org/baseR4/"},
            {"Local", "http://localhost:8000/fhir"}
        };        
        private static readonly string _fhirServer =  _fhirServers["PublicVonk"];
        
        //Main
        static int Main(string[] args)
        {
            var settings= new FhirClientSettings{
                PreferredFormat = ResourceFormat.Json,
                PreferredReturn = Prefer.ReturnRepresentation
            };

            FhirClient fhirClient = new FhirClient(_fhirServer, settings); //Cliente que establece conexión
            

            //CreatePatient(fhirClient, "Contreras", "Hugo");
            
            
            List<Patient> patients= GetPatients(fhirClient);

            System.Console.WriteLine($"Se ha encontrado: {patients.Count} pacientes!");

            string firstId= null; //Para guardar el primer id.

            foreach(Patient patient in patients)
            {
                if(string.IsNullOrEmpty(firstId))
                {
                    firstId= patient.Id;
                    continue;
                }
                DeletePatient(fhirClient, patient.Id);  //Elimina todos los pacientes excepto el primero.
            }

            Patient firstPatient = ReadPatient(fhirClient, firstId);

            System.Console.WriteLine($"Nombre de paciente leído: {firstPatient.Name[0].ToString()}");

            Patient updatedPatient= UpdatePatient(fhirClient, firstPatient);
            Patient readFinal = ReadPatient(fhirClient, firstId);

            return 0;
        }
        


        //Actualiza el paciente con más información.
        static Patient UpdatePatient(
            FhirClient fhirClient,
            Patient patient)
        {
            patient.Telecom.Add(new ContactPoint()
            {
                System = ContactPoint.ContactPointSystem.Phone,
                Value = "993087485",
                Use = ContactPoint.ContactPointUse.Home
            }); //Agrega información de contacto, número de teléfono y uso.

            patient.Gender = AdministrativeGender.Male; //Agrega el género.

            return fhirClient.Update<Patient>(patient);
        }



        //Lee el paciente de la id especificada.
        static Patient ReadPatient(
            FhirClient fhirClient,
            string id)
        {
            if(string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }
            Patient patient= fhirClient.Read<Patient>($"Patient/{id}");
            return patient;
        }


        //Consigue una lista de pacientes que coincida con los filtros entregados.
        static List<Patient> GetPatients(
            FhirClient fhirClient,
            string[] patientCriteria = null,
            int maxPatients = 12, //Máximo de búsqueda.
            bool onlyWithEncounters = false) //Flag para retornar sólo pacientes con encuentros.
        {
            List<Patient> patients = new List<Patient>();
            Bundle patientBundle;

            if(patientCriteria == null || (patientCriteria.Length == 0))
            {
                patientBundle = fhirClient.Search<Patient>(); //query REST de búsqueda por pacientes con nombre "test" 
            }
            else
            {
                patientBundle = fhirClient.Search<Patient>(patientCriteria); //query REST de búsqueda por pacientes con nombre "test"
            }

            while(patientBundle != null)
            {
                System.Console.WriteLine($"Total: {patientBundle.Total} Entry count: {patientBundle.Entry.Count}");

                foreach(Bundle.EntryComponent entry in patientBundle.Entry)
                {
                    if(entry.Resource != null) //Validación que recurso en bundle no sea nulo.
                    {
                        Patient patient = (Patient)entry.Resource;

                        Bundle encounterBundle= fhirClient.Search<Encounter>( //Obtener los encuentros para un paciente del bundle.
                            new string[]
                            {
                                $"patient=Patient/{patient.Id}"
                            });
                        if (onlyWithEncounters && (encounterBundle.Total == 0))
                        {
                            continue;
                        }

                        patients.Add(patient);

                        
                        System.Console.WriteLine($"- Entry: {patients.Count, 3} {entry.FullUrl}");
                        System.Console.WriteLine($" -     IdPaciente: {patient.Id,20}");

                        if(patient.Name.Count > 0 ) //Si hay nombres en la lista de nombres
                        {
                            System.Console.WriteLine($" - NombrePaciente: {patient.Name[0].ToString()}");
                        }
                        
                        if(encounterBundle.Total > 0)
                        {
                            System.Console.WriteLine($" - Encounters totales: {encounterBundle.Total} Entry count: {encounterBundle.Entry.Count}");
                        }
                        
                    }

                    if(patients.Count >= maxPatients)
                    {
                        break;
                    }
                }

                if(patients.Count >= maxPatients)
                {
                    break;
                }
                //Obtener más resultados (paginación del Bundle)
                patientBundle= fhirClient.Continue(patientBundle);
            }
            return patients;
        }
        

        //Crea un paciente con el nombre especificado.
        static void CreatePatient(
            FhirClient fhirClient,
            string familyName,
            string givenName)
        {
            Patient toCreate = new Patient()
            {
                Name= new List<HumanName>()
                {
                    new HumanName()
                    {
                        Family = familyName,
                        Given = new List<string>()
                        {
                            givenName
                        }
                    }
                },
                BirthDateElement = new Date(1998, 30, 01)
            };

            Patient created= fhirClient.Create<Patient>(toCreate);
            System.Console.WriteLine($"Creado el paciente {created.Id}");
        }
    

        //Elimina un paciente con el id especificado.  
        static void DeletePatient(
            FhirClient fhirClient,
            string id)
        {
            if(string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }
            System.Console.WriteLine($"Borrando el paciente {id}");
            fhirClient.Delete($"Patient/{id}");
        }
    }
}

