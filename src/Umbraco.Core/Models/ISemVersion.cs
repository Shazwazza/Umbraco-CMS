using System;

namespace Umbraco.Core.Models
{
    public class SemVersion : ISemVersion
    {
        public SemVersion(Version s)
        {
            throw new NotImplementedException("Need to sort out the aspnetcore semver assembly");
        }
        public SemVersion(string s)
        {
            throw new NotImplementedException("Need to sort out the aspnetcore semver assembly");
        }
        public SemVersion(int s)
        {
            throw new NotImplementedException("Need to sort out the aspnetcore semver assembly");
        }

        public SemVersion(int major, int minor, int build, string s, string s1)
        {
            throw new NotImplementedException("Need to sort out the aspnetcore semver assembly");
        }

        public static bool TryParse(string s, out ISemVersion sv)
        {
            throw new NotImplementedException("Need to sort out the aspnetcore semver assembly");            
        }
        public static ISemVersion Parse(string s)
        {
            throw new NotImplementedException("Need to sort out the aspnetcore semver assembly");
        }
    }

    public interface ISemVersion
    {
        //TODO: Fill this out!
    }
}