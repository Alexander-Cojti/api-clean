using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System.Configuration;

//'CosmosDBAttribute.ConnectionStringSetting'. 
//prueba subir una linea
namespace envio_cosmos
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static  IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, [CosmosDB(databaseName: "taskDatabase",
                collectionName: "TaskCollection",
                ConnectionStringSetting = "MyCosmosDBConnection",
                CreateIfNotExists=true)] out dynamic document, ILogger log)
        {
            log.LogInformation("C# HTTP TRIGGER PROCESADO Y CONECTADO A COSMOS");
            var searchServiceName = "jfk-search-service-qymjpzort5hho";
            var apiKey = "023DB82430FFA416AD39EEF8A6FDFF2A";
            log.LogInformation("API'S AZURE SEARCH ACEPTADAS");
            var searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(apiKey));
            var indexClient = searchClient.Indexes.GetClient("jfkindex");
            //var indexClient = searchClient.Indexes.GetClient("jfkindex");
            SearchParameters sp = new SearchParameters() { SearchMode = SearchMode.All };

            string name = req.Query["name"];
            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;
            log.LogInformation($"OBTENIENDO EL PARAMETRO DE BUSQUEDA: {name}");
            //generar el json del buscador
            var docs = indexClient.Documents.Search("\"" + name + "\"", sp);
            var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(docs.Results);
            log.LogInformation("SERIALIZANDO JSON OBTENIDO");
            dynamic jsonObj = JsonConvert.DeserializeObject(jsonString);//decerealizar el json 
            log.LogInformation("DECERIALIZANDO JSON OBTENIDO PARA RECORRERLO :D");
            string nombreDocumento = (jsonObj[0]["Document"]["fileName"]);
            string texto = (jsonObj[0]["Document"]["text"]);//recorrer el json y obtener todo lo que se encuentre
            log.LogInformation("RECORRIDO DEL JSON HASTA -text- COMPLETADO :D");

            //inicia creacion de subcadena
            string codigoEmpresa = (texto.Substring(texto.IndexOf("Pago "))).Substring(5, 3);//se posisiciona hasta llegar al valor que se encuentrea en comillas y crea una subcadena desde ese momento

            //finaliza la creacion de subcadena
            string numeroPlanilla = (texto.Substring(texto.IndexOf("Pago "))).Substring(8, 6);//se posisiciona hasta llegar al valor que se encuentrea en comillas y crea una subcadena desde ese momento


            //Inicio Funion regex
            Regex exprecionNitEmpresa = new Regex(@"\b[0-9]\d{3}(-\d{1})\b");//Expresion regular que encuentra el nit de la empresa em un array
            Match nitEmpresa = exprecionNitEmpresa.Match(texto);// Realiza la comparacion de la exprecion regular con 

            //Finaliza Funcion Regex
            string codigoDepartamento = (texto.Substring(texto.IndexOf("Depto."))).Substring(7, 5);

            //inicia Abstraccion json 
            int inicioNombreDepartamento = texto.IndexOf("("), finalizaNombreDepartamento = texto.IndexOf(")");  //se define un rango con 2 palabras para generar una subcadena
            string nombreDepartamento = texto.Substring(inicioNombreDepartamento + 1, finalizaNombreDepartamento - inicioNombreDepartamento - 1);//se define el rango entre ambos strings

            //finaliza Abstraccion json 
            //inicia creacion de subcadena
            string numeroBoleta = (texto.Substring(texto.IndexOf("Boleta:"))).Substring(8, 10);//se posisiciona hasta llegar al valor que se encuentrea en comillas y crea una subcadena desde ese momento

            //finaliza la creacion de subcadena
            //inicia creacion de subcadena
            string fechaPago = (texto.Substring(texto.IndexOf("Fecha:"))).Substring(7, 10);//se posisiciona hasta llegar al valor que se encuentrea en comillas y crea una subcadena desde ese momento

            //finaliza la creacion de subcadena
            //Inicio Funion regex
            Regex exprecionCodigoEmpleado = new Regex(@"\b[A-Z]{3}[0-9]{7}\b");//Expresion regular que encuentra el nit de la empresa em un array
            Match codigoEmpleado = exprecionCodigoEmpleado.Match(texto);// Realiza la comparacion de la exprecion regular con 

            //Finaliza Funcion Regex
            //Inicio Funion regex
            Regex exprecionNombreEmpleado = new Regex(@"\b[A-Z]{3,15}\s[A-Z]{3,15}\s[A-Z]{3,15}\b");//Expresion regular que encuentra el nit de la empresa em un array
            Match nombreEmpleado = exprecionNombreEmpleado.Match(texto);// Realiza la comparacion de la exprecion regular con 

            //Finaliza Funcion Regex
            //Inicio Funion regex
            Regex exprecionNitEmpleado = new Regex(@"\b[A0-9]{7}-[0-9]{1}\b");//Expresion regular que encuentra el nit de la empresa em un array
            Match nitEmpleado = exprecionNitEmpleado.Match(texto);// Realiza la comparacion de la exprecion regular con 
            log.LogInformation("SELECCION DE DATOS ESPECIFICOS COMPLETADOS :D");
            //  document = new { CodigoEmpresa = codigoEmpresa, NumeroPlanilla = numeroPlanilla, codigoDepartamento = CodigoDepartamento, horaInsercion = DateTime.Now };
            document = new
            {
                NombreDocumento = nombreDocumento,
                CodigoEmpresa = codigoEmpresa,
                NumeroPlanila = numeroPlanilla,
                NitEmpresa = nitEmpresa.Value,
                CodigoDepartamento = codigoDepartamento,
                NombreDepartamento = nombreDepartamento,
                NumeroBoleta = numeroBoleta,
                FechaPago = fechaPago,
                CodigoColaborador = codigoEmpleado.Value,
                NombreColaborador = nombreEmpleado.Value,
                NitColaborador = nitEmpleado.Value,
                FechaInsercion =  DateTime.Now
            };
            log.LogInformation("ENVIO A COSMOS COMPLETADO :D");
            return name != null
          
                ? (ActionResult)new OkObjectResult($"Nombre del Documento: {nombreDocumento}" + "\n" + $"Codigo Empresa: {codigoEmpresa}" + "\n" + $"Numero Planilla: {numeroPlanilla}" + "\n" + $"Nit Empresa: {nitEmpresa}" + "\n" + $"Codigo Departamento: {codigoDepartamento}" + "\n" + $"Nombre Departamento De Empresa: {nombreDepartamento}" + "\n" + $"Numero Boleta: {numeroBoleta}" + "\n" + $"Fecha Pago planilla: {fechaPago}" + "\n" + $"Codigo Colaborador: {codigoEmpleado}" + "\n" + $"Nombre Colaborador: {nombreEmpleado}" + "\n" + $"Nit Colaborador: {nitEmpleado}")
                : new BadRequestObjectResult("porfavor ingrese un nombre para enviar a la base de datos de cosmos :)");
            
        }
    }
}

/* 
 ------------------------------ funcion final cosmos -----------------------
 using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System.Configuration;

//'CosmosDBAttribute.ConnectionStringSetting'. 

namespace envio_cosmos
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static  IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, [CosmosDB(databaseName: "taskDatabase",
                collectionName: "TaskCollection",
                ConnectionStringSetting = "MyCosmosDBConnection",
                CreateIfNotExists=true)] out dynamic document, ILogger log)
        {
            log.LogInformation("C# HTTP TRIGGER PROCESADO Y CONECTADO A COSMOS");
            var searchServiceName = "jfk-search-service-qymjpzort5hho";
            var apiKey = "023DB82430FFA416AD39EEF8A6FDFF2A";
            log.LogInformation("API'S AZURE SEARCH ACEPTADAS");
            var searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(apiKey));
            var indexClient = searchClient.Indexes.GetClient("jfkindex");
            SearchParameters sp = new SearchParameters() { SearchMode = SearchMode.All };

            string name = req.Query["name"];
            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;
            log.LogInformation($"OBTENIENDO EL PARAMETRO DE BUSQUEDA: {name}");
            //generar el json del buscador
            var docs = indexClient.Documents.Search("\"" + name + "\"", sp);
            var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(docs.Results);
            log.LogInformation("SERIALIZANDO JSON OBTENIDO");
            dynamic jsonObj = JsonConvert.DeserializeObject(jsonString);//decerealizar el json 
            log.LogInformation("DECERIALIZANDO JSON OBTENIDO PARA RECORRERLO :D");
            string nombreDocumento = (jsonObj[0]["Document"]["fileName"]);
            string texto = (jsonObj[0]["Document"]["text"]);//recorrer el json y obtener todo lo que se encuentre
            log.LogInformation("RECORRIDO DEL JSON HASTA -text- COMPLETADO :D");

            //inicia creacion de subcadena
            string codigoEmpresa = (texto.Substring(texto.IndexOf("Pago "))).Substring(5, 3);//se posisiciona hasta llegar al valor que se encuentrea en comillas y crea una subcadena desde ese momento

            //finaliza la creacion de subcadena
            string numeroPlanilla = (texto.Substring(texto.IndexOf("Pago "))).Substring(8, 6);//se posisiciona hasta llegar al valor que se encuentrea en comillas y crea una subcadena desde ese momento


            //Inicio Funion regex
            Regex exprecionNitEmpresa = new Regex(@"\b[0-9]\d{3}(-\d{1})\b");//Expresion regular que encuentra el nit de la empresa em un array
            Match nitEmpresa = exprecionNitEmpresa.Match(texto);// Realiza la comparacion de la exprecion regular con 

            //Finaliza Funcion Regex
            string codigoDepartamento = (texto.Substring(texto.IndexOf("Depto."))).Substring(7, 5);

            //inicia Abstraccion json 
            int inicioNombreDepartamento = texto.IndexOf("("), finalizaNombreDepartamento = texto.IndexOf(")");  //se define un rango con 2 palabras para generar una subcadena
            string nombreDepartamento = texto.Substring(inicioNombreDepartamento + 1, finalizaNombreDepartamento - inicioNombreDepartamento - 1);//se define el rango entre ambos strings

            //finaliza Abstraccion json 
            //inicia creacion de subcadena
            string numeroBoleta = (texto.Substring(texto.IndexOf("Boleta:"))).Substring(8, 10);//se posisiciona hasta llegar al valor que se encuentrea en comillas y crea una subcadena desde ese momento

            //finaliza la creacion de subcadena
            //inicia creacion de subcadena
            string fechaPago = (texto.Substring(texto.IndexOf("Fecha:"))).Substring(7, 10);//se posisiciona hasta llegar al valor que se encuentrea en comillas y crea una subcadena desde ese momento

            //finaliza la creacion de subcadena
            //Inicio Funion regex
            Regex exprecionCodigoEmpleado = new Regex(@"\b[A-Z]{3}[0-9]{7}\b");//Expresion regular que encuentra el nit de la empresa em un array
            Match codigoEmpleado = exprecionCodigoEmpleado.Match(texto);// Realiza la comparacion de la exprecion regular con 

            //Finaliza Funcion Regex
            //Inicio Funion regex
            Regex exprecionNombreEmpleado = new Regex(@"\b[A-Z]{3,15}\s[A-Z]{3,15}\s[A-Z]{3,15}\b");//Expresion regular que encuentra el nit de la empresa em un array
            Match nombreEmpleado = exprecionNombreEmpleado.Match(texto);// Realiza la comparacion de la exprecion regular con 

            //Finaliza Funcion Regex
            //Inicio Funion regex
            Regex exprecionNitEmpleado = new Regex(@"\b[A0-9]{7}-[0-9]{1}\b");//Expresion regular que encuentra el nit de la empresa em un array
            Match nitEmpleado = exprecionNitEmpleado.Match(texto);// Realiza la comparacion de la exprecion regular con 
            log.LogInformation("SELECCION DE DATOS ESPECIFICOS COMPLETADOS :D");
            //  document = new { CodigoEmpresa = codigoEmpresa, NumeroPlanilla = numeroPlanilla, codigoDepartamento = CodigoDepartamento, horaInsercion = DateTime.Now };
            document = new
            {
                NombreDocumento = nombreDocumento,
                CodigoEmpresa = codigoEmpresa,
                NumeroPlanila = numeroPlanilla,
                NitEmpresa = nitEmpresa.Value,
                CodigoDepartamento = codigoDepartamento,
                NombreDepartamento = nombreDepartamento,
                NumeroBoleta = numeroBoleta,
                FechaPago = fechaPago,
                CodigoColaborador = codigoEmpleado.Value,
                NombreColaborador = nombreEmpleado.Value,
                NitColaborador = nitEmpleado.Value,
                FechaInsercion =  DateTime.Now
            };
            log.LogInformation("ENVIO A COSMOS COMPLETADO :D");
            return name != null
          
                ? (ActionResult)new OkObjectResult($"Nombre del Documento: {nombreDocumento}" + "\n" + $"Codigo Empresa: {codigoEmpresa}" + "\n" + $"Numero Planilla: {numeroPlanilla}" + "\n" + $"Nit Empresa: {nitEmpresa}" + "\n" + $"Codigo Departamento: {codigoDepartamento}" + "\n" + $"Nombre Departamento De Empresa: {nombreDepartamento}" + "\n" + $"Numero Boleta: {numeroBoleta}" + "\n" + $"Fecha Pago planilla: {fechaPago}" + "\n" + $"Codigo Colaborador: {codigoEmpleado}" + "\n" + $"Nombre Colaborador: {nombreEmpleado}" + "\n" + $"Nit Colaborador: {nitEmpleado}")
                : new BadRequestObjectResult("porfavor ingrese un nombre para enviar a la base de datos de cosmos :)");
            
        }
    }
}
 */
/*
         [FunctionName("Function1")]
    public static async Task<HttpResponseMessage> Run(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req,
    TraceWriter log)
    {
        dynamic data = await req.Content.ReadAsAsync<object>();

        var connectionString = "DbUri";
        var key = "DbKey";

        using (var client = new DocumentClient(new Uri(connectionString), key))
        {
            var collectionLink = UriFactory.CreateDocumentCollectionUri("DbName", "CollectionName");
            await client.UpsertDocumentAsync(collectionLink, data);
        }

        return req.CreateResponse(HttpStatusCode.OK);
    }
 */

/*
 * namespace envio_cosmos
{
public static class Function1
{
    [FunctionName("Function1")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        string name = req.Query["name"];

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);
        name = name ?? data?.name;

        return name != null
            ? (ActionResult)new OkObjectResult($"Hello, {name}")
            : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
    }

    */

