using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http.Dispatchers;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Profiles.Qualities
{
    public interface IProfileService
    {
        Profile Add(Profile profile);
        void Update(Profile profile);
        void Delete(int id);
        List<Profile> All();
        Profile Get(int id);
        bool Exists(int id);
        Profile GetDefaultProfile(string name, Quality cutoff = null, params Quality[] allowed);

    }

    public class ProfileService : IProfileService, IHandle<ApplicationStartedEvent>
    {
        private readonly IProfileRepository _profileRepository;
        private readonly IArtistService _artistService;
        private readonly Logger _logger;

        public ProfileService(IProfileRepository profileRepository, IArtistService artistService, Logger logger)
        {
            _profileRepository = profileRepository;
            _artistService = artistService;
            _logger = logger;
        }

        public Profile Add(Profile profile)
        {
            return _profileRepository.Insert(profile);
        }

        public void Update(Profile profile)
        {
            _profileRepository.Update(profile);
        }

        public void Delete(int id)
        {
            if (_artistService.GetAllArtists().Any(c => c.ProfileId == id))
            {
                throw new ProfileInUseException(id);
            }

            _profileRepository.Delete(id);
        }

        public List<Profile> All()
        {
            return _profileRepository.All().ToList();
        }

        public Profile Get(int id)
        {
            return _profileRepository.Get(id);
        }

        public bool Exists(int id)
        {
            return _profileRepository.Exists(id);
        }

        public void Handle(ApplicationStartedEvent message)
        {
            if (All().Any()) return;

            _logger.Info("Setting up default quality profiles");

            AddDefaultProfile("Any", Quality.Unknown,
                Quality.Unknown,
                Quality.MP3_008,
                Quality.MP3_016,
                Quality.MP3_024,
                Quality.MP3_032,
                Quality.MP3_040,
                Quality.MP3_048,
                Quality.MP3_056,
                Quality.MP3_064,
                Quality.MP3_080,
                Quality.MP3_096,
                Quality.MP3_112,
                Quality.MP3_128,
                Quality.MP3_160,
                Quality.MP3_192,
                Quality.MP3_224,
                Quality.MP3_256,
                Quality.MP3_320,
                Quality.MP3_VBR,
                Quality.MP3_VBR_V2,
                Quality.AAC_192,
                Quality.AAC_256,
                Quality.AAC_320,
                Quality.AAC_VBR,
                Quality.VORBIS_Q5,
                Quality.VORBIS_Q6,
                Quality.VORBIS_Q7,
                Quality.VORBIS_Q8,
                Quality.VORBIS_Q9,
                Quality.VORBIS_Q10,
                Quality.WMA,
                Quality.ALAC,
                Quality.FLAC,
                Quality.FLAC_24);

            AddDefaultProfile("Lossless", Quality.FLAC,
                Quality.FLAC,
                Quality.ALAC,
                Quality.FLAC_24);

            AddDefaultProfile("Standard", Quality.MP3_192,
                Quality.MP3_192,
                Quality.MP3_256,
                Quality.MP3_320);
        }

        public Profile GetDefaultProfile(string name, Quality cutoff = null, params Quality[] allowed)
        {
            var groupedQualites = Quality.DefaultQualityDefinitions.GroupBy(q => q.GroupWeight);
            var items = new List<ProfileQualityItem>();
            var groupId = 1000;
            var profileCutoff = cutoff == null ? Quality.Unknown.Id : cutoff.Id;

            foreach (var group in groupedQualites)
            {
                if (group.Count() == 1)
                {
                    var quality = group.First().Quality;
                    items.Add(new ProfileQualityItem { Quality = quality, Allowed = allowed.Contains(quality) });
                    continue;
                }

                var groupAllowed = group.Any(g => allowed.Contains(g.Quality));

                items.Add(new ProfileQualityItem
                {
                    Id = groupId,
                    Name = group.First().GroupName,
                    Items = group.Select(g => new ProfileQualityItem
                    {
                        Quality = g.Quality,
                        Allowed = groupAllowed
                    }).ToList(),
                    Allowed = groupAllowed
                });

                if (group.Any(s => s.Quality.Id == profileCutoff))
                {
                    profileCutoff = groupId;
                }

                groupId++;
            }

            var qualityProfile = new Profile
            {
                Name = name,
                Cutoff = profileCutoff,
                Items = items
            };

            return qualityProfile;
        }

        private Profile AddDefaultProfile(string name, Quality cutoff, params Quality[] allowed)
        {
            var profile = GetDefaultProfile(name, cutoff, allowed);

            return Add(profile);
        }
    }
}
