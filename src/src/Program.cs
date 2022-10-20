using System;
using System.Collections.Generic;
using Hl7.Fhir.Model; //Contiene los tipos de datos de los recursos FHIR.
using Hl7.Fhir.Rest; //Para llamadas HTTP y cliente FHIR.
using Hl7.Fhir.Serialization;

namespace fhir_cs_tutorial
{
    //Main
    public static class Program
    {
        private const string _fhirServer =  "http://vonk.fire.ly";
        static void Main(string[] args)
        {
            var settings= new FhirClientSettings{
                PreferredFormat = ResourceFormat.Json,
                PreferredReturn = Prefer.ReturnRepresentation
            };

            FhirClient fhirClient = new FhirClient(_fhirServer, settings); //Cliente que establece conexión

            Bundle patientBundle = fhirClient.Search<Patient>(new string[]{"name=test"}); //query REST de búsqueda por pacientes con nombre "test" 

            int patientNumber= 0;
            List<string> patientsWithEncounters= new List<string>();

            while(patientBundle != null)
            {
                System.Console.WriteLine($"Total: {patientBundle.Total} Entry count: {patientBundle.Entry.Count}");

                foreach(Bundle.EntryComponent entry in patientBundle.Entry)
                {
                    //
                    

                    if(entry.Resource != null) //Validación que recurso en bundle no sea nulo.
                    {
                        Patient patient = (Patient)entry.Resource;

                        Bundle encounterBundle= fhirClient.Search<Encounter>( //Obtener los encuentros para un paciente del bundle.
                            new string[]
                            {
                                $"patient=Patient/{patient.Id}"
                            });
                        if (encounterBundle.Total == 0)
                        {
                            continue;
                        }

                        patientsWithEncounters.Add(patient.Id);

                        
                        System.Console.WriteLine($"- Entry: {patientNumber, 3} {entry.FullUrl}");
                        System.Console.WriteLine($" -     IdPaciente: {patient.Id,20}");

                        if(patient.Name.Count > 0 ) //Si hay nombres en la lista de nombres
                        {
                            System.Console.WriteLine($" - NombrePaciente: {patient.Name[0].ToString()}");
                        }
                        
                        
                        System.Console.WriteLine($" - Encounters totales: {encounterBundle.Total} Entry count: {encounterBundle.Entry.Count}");
                    }
                    patientNumber++;

                    if(patientsWithEncounters.Count >= 1)
                    {
                        break;
                    }
                }
                if(patientsWithEncounters.Count >= 1)
                {
                    break;
                }
                //Obtener más resultados (paginación del Bundle)
                patientBundle= fhirClient.Continue(patientBundle);
            }
        }
    }
}

