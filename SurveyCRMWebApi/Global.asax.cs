using log4net;
using log4net.Config;
using log4net.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace SurveyCRMWebApiV2
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        public static ILoggerRepository repository { get; set; }


        protected void Application_Start()
        {

            ////log
            //repository = LogManager.CreateRepository("SurveyWebAPIV2Log");
            //XmlConfigurator.Configure(repository, new FileInfo("log4net.config"));
            //BasicConfigurator.Configure(repository);
            ////log4net.Config.XmlConfigurator.Configure();

            GlobalConfiguration.Configure(WebApiConfig.Register);
           
        }
    }
}