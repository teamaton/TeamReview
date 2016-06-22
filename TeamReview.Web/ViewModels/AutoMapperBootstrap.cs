using AutoMapper;
using TeamReview.Core;
using TeamReview.Core.Models;
using TeamReview.Core.ViewModels;

namespace TeamReview.Web.ViewModels {
	public class AutoMapperBootstrap {
		public static void Initialize() {
			Mapper.CreateMap<ReviewConfiguration, ReviewCreateEditModel>()
				.ForMember(model => model.ExistingCategories, opt => opt.ResolveUsing(review => review.Categories))
				.ForMember(model => model.ExistingPeers, opt => opt.ResolveUsing(review => review.Peers))
				.ReverseMap();
			Mapper.CreateMap<ReviewCategory, CategoryAddModel>().ReverseMap();
			Mapper.CreateMap<ReviewCategory, CategoryShowModel>();
			Mapper.CreateMap<UserProfile, PeerAddModel>().ReverseMap();
			Mapper.CreateMap<UserProfile, PeerShowModel>();
			Mapper.CreateMap<Report, ReportViewModel>();
		}
	}
}