using System.Web.Mvc;
using Autofac;
using Autofac.Integration.Mvc;
using TeamReview.Core.DataAccess;
using TeamReview.Core.Services;

namespace TeamReview.Web {
	public static class AutofacConfig {
		public static void RegisterDependencies() {
			var builder = RegisterMvcRelatedDependencies();
			RegisterCustomDependencies(builder);
			BuildAndRegisterAutofacContainer(builder);
		}

		private static ContainerBuilder RegisterMvcRelatedDependencies() {
			var builder = new ContainerBuilder();
			builder.RegisterControllers(typeof (MvcApplication).Assembly);
			builder.RegisterModelBinders(typeof (MvcApplication).Assembly);
			builder.RegisterModelBinderProvider();
			return builder;
		}

		private static void RegisterCustomDependencies(ContainerBuilder builder) {
			builder.RegisterAssemblyTypes(typeof (IDatabaseContext).Assembly).AsImplementedInterfaces();
		    builder.RegisterType<LiteSmtpClient>().As<ISmtpClient>();
		}

		private static void BuildAndRegisterAutofacContainer(ContainerBuilder builder) {
			var container = builder.Build();
			DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
		}
	}
}