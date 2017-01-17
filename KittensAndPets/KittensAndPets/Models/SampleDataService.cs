using System;
using System.Collections.ObjectModel;
using Microsoft.Toolkit.Uwp.Services.Bing;
using Microsoft.Toolkit.Uwp.Services.Twitter;

namespace KittensAndPets.Models
{
    public static class SampleDataService
    {
        public static ObservableCollection<BingResult> GenerateBingResults()
        {
            var results = new ObservableCollection<BingResult>();

            for (int i = 0; i < 20; i++)
            {
                results.Add(new BingResult
                {
                    Title = $"Result {i} of a Bing Search",
                    Summary = "This is some longer text that comes along with the title",
                    Link = "www.bing.com/blah/blabba/blabbity/blah.html",
                    Published = DateTime.Now
                });
            }

            return results;
        }

        public static ObservableCollection<Tweet> GenerateTweets()
        {
            var tweets = new ObservableCollection<Tweet>();

            for (int i = 0; i < 20; i++)
            {
                tweets.Add(new Tweet
                {
                    Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec efficitur, enim at egestas posuere, est enim posuere lorem, in pretium metus.",
                    User = GenerateUser(),
                    CreatedAt = DateTime.Now.ToString("g")
                });
            }

            return tweets;
        }

        public static TwitterUser GenerateUser()
        {
            return new TwitterUser
            {
                Name = $"Kittens and Pets",
                ScreenName = "@Kittenz",
                ProfileImageUrl = "https://pbs.twimg.com/profile_images/812868055265148928/xQ8kieAU_400x400.jpg"
            };
        }
    }
}
