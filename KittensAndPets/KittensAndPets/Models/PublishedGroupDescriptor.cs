using Microsoft.Toolkit.Uwp.Services.Bing;
using Telerik.Data.Core;

namespace KittensAndPets.Models
{
    public class PublishedGroupDescriptor : IKeyLookup
    {
        public object GetKey(object instance)
        {
            var key = instance as BingResult;

            return key?.Published.ToString("d");
        }
    }
}
