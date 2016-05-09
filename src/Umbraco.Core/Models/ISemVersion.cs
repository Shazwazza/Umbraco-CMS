using System;

namespace Umbraco.Core.Models
{
    public class SemVersion
    {
        public static bool TryParse(string s, out ISemVersion sv)
        {
            throw new NotImplementedException("Need to sort out the aspnetcore semver assembly");            
        }
    }

    public interface ISemVersion
    {
        //TODO: Fill this out!
    }
}