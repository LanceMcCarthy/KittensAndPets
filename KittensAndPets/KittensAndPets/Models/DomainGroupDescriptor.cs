using System;
using Microsoft.Toolkit.Uwp.Services.Bing;
using Telerik.Data.Core;

namespace KittensAndPets.Models
{
    public class DomainGroupDescriptor : IKeyLookup
    {
        public object GetKey(object instance)
        {
            var item = instance as BingResult;

            return new Uri(item?.Link).Host;
        }
    }
}
