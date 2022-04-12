using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ThinkInAspNetCore.MiniMvc.Extensions
{
    public static class MvcExtensions
    {
        private static Dictionary<string, Type> controller2Type;
        private static Dictionary<string, Dictionary<string, MethodInfo>> controllerAction2Method;

        static MvcExtensions()
        {
            controller2Type = new Dictionary<string, Type>();
            controllerAction2Method = new Dictionary<string, Dictionary<string, MethodInfo>>();
            Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var tempType in allTypes)
            {
                if (!tempType.IsAbstract && Regex.IsMatch(tempType.FullName, @"(?is).+Controller$"))
                {
                    string name = tempType.Name.Replace("Controller", "").ToLower();
                    controller2Type[name] = tempType;
                    controllerAction2Method[name] = new Dictionary<string, MethodInfo>();
                    foreach (var mi in tempType.GetMethods())
                    {
                        if (mi.IsPublic && !mi.IsStatic && !mi.IsConstructor)
                        {
                            controllerAction2Method[name][mi.Name.ToLower()] = mi;
                        }
                    }
                }
            }
        }

        public static WebApplication UseMvc(this WebApplication webApplication)
        {
            webApplication.Use((context, _next) =>
            {
                string requestUrl = context.Request.RequstUrl.ToLower();
                string[] routeArr = requestUrl.Split("/", StringSplitOptions.RemoveEmptyEntries);
                string controllerName = string.Empty;
                string actionName = string.Empty;
                if (routeArr.Length >= 2)
                {
                    controllerName = routeArr[0].Trim();
                    actionName = routeArr[1].Trim();
                }
                if (string.IsNullOrEmpty(controllerName))
                {
                    controllerName = "home";
                }
                if (string.IsNullOrEmpty(actionName))
                {
                    actionName = "index";
                }

                Type type = null;
                if (controller2Type.ContainsKey(controllerName))
                {
                    type = controller2Type[controllerName];
                }
                if (type != null)
                {
                    var controllerObj = Activator.CreateInstance(type);
                    type.GetProperty("Request").SetValue(controllerObj, context.Request);
                    type.GetProperty("Response").SetValue(controllerObj, context.Response);
                    MethodInfo actionMem = null;
                    if (controllerAction2Method[controllerName].ContainsKey(actionName))
                    {
                        actionMem = controllerAction2Method[controllerName][actionName];
                    }
                    if (actionMem != null)
                    {
                        actionMem.Invoke(controllerObj, null);
                    }
                    else
                    {
                        context.Response.StatusCode = "404";
                        context.Response.StatusMessage = "Not Found";
                    }
                }
                else
                {
                    context.Response.StatusCode = "404";
                    context.Response.StatusMessage = "Not Found";
                }
                return Task.CompletedTask;
            });
            return webApplication;
        }
    }
}
