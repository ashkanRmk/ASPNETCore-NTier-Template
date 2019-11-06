using System.Collections;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Liaro.ServiceLayer.Security
{
    public static class ModelStateHelper
    {
        public static IEnumerable DictionaryErrors(this ModelStateDictionary modelState)
        {
            if (modelState.IsValid) return null;
            var errors = new Hashtable();
            foreach (var pair in modelState)
            {
                if (pair.Value.Errors.Count > 0)
                {
                    var message = pair.Value.Errors.Select(error => error.ErrorMessage).First();
                    try
                    {
                        var hashtableMsg = Newtonsoft.Json.JsonConvert.DeserializeObject<Hashtable>(message);
                        if (hashtableMsg != null)
                            errors = hashtableMsg;
                    }
                    catch
                    {
                        errors[pair.Key] = message;
                    }
                }
            }
            return errors;
        }
    }
}