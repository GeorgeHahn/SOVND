using SOVND.Lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Should;

namespace SOVND.Lib.Tests
{
    public class SongTests
    {
        [Fact]
        public void SongsShouldSortByVotes()
        {
            var songs = new List<Song>();

            var top = new Song("test") {Votes = 4};
            var mid = new Song("test") {Votes = 3};
            var bot = new Song("test") {Votes = 1};

            songs.Add(bot);
            songs.Add(top);
            songs.Add(mid);

            songs.Sort();

            songs[0].ShouldBe(top);
            songs[1].ShouldBe(mid);
            songs[2].ShouldBe(bot);
        }


        [Fact]
        public void SongsShouldSortByVotetime()
        {
            var songs = new List<Song>();

            var top = new Song("test") {Votetime = 1417576976};
            var mid = new Song("test") {Votetime = 1417577976};
            var bot = new Song("test") {Votetime = 1417578976};

            songs.Add(bot);
            songs.Add(top);
            songs.Add(mid);

            songs.Sort();

            songs[0].ShouldBe(top);
            songs[1].ShouldBe(mid);
            songs[2].ShouldBe(bot);
        }


        [Fact]
        public void SongsShouldSortByBoth()
        {
            var songs = new List<Song>();

            var top = new Song("test") {Votes = 2, Votetime = 1417579976};
            var mid = new Song("test") {Votes = 1, Votetime = 1417576976};
            var bot = new Song("test") {Votes = 1, Votetime = 1417577976};

            songs.Add(bot);
            songs.Add(top);
            songs.Add(mid);

            songs.Sort();

            songs[0].ShouldBe(top);
            songs[1].ShouldBe(mid);
            songs[2].ShouldBe(bot);
        }
    }
}