using Microsoft.AnalysisServices.AdomdClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;

namespace API_REST_Cubo_Northwind.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("v1/Analysis/Northwind")]
    public class NorthwindController : ApiController
    {
        [HttpGet]
        [Route("Testing")]
        public HttpResponseMessage Testing()
        {
            return Request.CreateResponse(HttpStatusCode.OK, "Prueba de API Exitosa");
        }

        [HttpGet]
        [Route("Top5/{dim}/{order}")]
        public HttpResponseMessage Top5(string dim, string order = "DESC")
        {
            string dimension = string.Empty;

            switch(dim) {
                case "Cliente": dimension = "[Dim Cliente].[Dim Cliente Nombre].CHILDREN";
                    break;
                case "Producto": dimension = "[Dim Producto].[Dim Producto Nombre].CHILDREN";
                    break;
                case "Empleado": dimension = "[Dim Empleado].[Dim Empleado Nombre].CHILDREN";
                    break;
                default:
                    dimension = "[Dim Cliente].[Dim Cliente Nombre].CHILDREN";
                    break;
            }

            string WITH = @"
                WITH 
                SET [TopVentas] AS 
                NONEMPTY(
                    ORDER(
                        STRTOSET(@Dimension),
                        [Measures].[Fact Ventas Netas], " + order +
                    @")
                )
            ";

            string COLUMNS = @"
                NON EMPTY
                {
                    [Measures].[Fact Ventas Netas]
                }
                ON COLUMNS,    
            ";

            string ROWS = @"
                NON EMPTY
                {
                    HEAD([TopVentas], 5)
                }
                ON ROWS
            ";

            string CUBO_NAME = "[DWH Northwind]";
            string MDX_QUERY = WITH + @"SELECT " + COLUMNS + ROWS + " FROM " + CUBO_NAME;

            Debug.Write(MDX_QUERY);

            List<string> clients = new List<string>();
            List<decimal> ventas = new List<decimal>();
            List<dynamic> lstTabla = new List<dynamic>();

            dynamic result = new
            {
                datosDimension = clients,
                datosVenta = ventas,
                datosTabla = lstTabla
            };

            using(AdomdConnection cnn = new AdomdConnection(ConfigurationManager.ConnectionStrings["CuboNorthwind"].ConnectionString)) {
                cnn.Open();
                using(AdomdCommand cmd = new AdomdCommand(MDX_QUERY, cnn)) {
                    cmd.Parameters.Add("Dimension", dimension);
                    using(AdomdDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection)) {
                        while(dr.Read()) {
                            clients.Add(dr.GetString(0));
                            ventas.Add(Math.Round(dr.GetDecimal(1)));

                            dynamic objTabla = new
                            {
                                descripcion = dr.GetString(0),
                                valor = Math.Round(dr.GetDecimal(1))
                            };

                            lstTabla.Add(objTabla);
                        }
                        dr.Close();
                    }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, (object)result);
        }

    }
}
