using System.Reflection;
using FluentAssertions;
using PetShop.Service.Security;
using PetShop.Service.Services;
using Xunit;

namespace PetShop.Tests.Architecture;

/// <summary>
/// Enforces the rule that <b>every</b> service in the service layer receives the current
/// user. <see cref="ServiceBase"/> guarantees the dependency for classes that inherit it,
/// but the compiler cannot force a new service to inherit — so these reflection tests are
/// the backstop that fails CI when someone forgets.
/// </summary>
public class ServiceLayerArchitectureTests
{
    // Concrete "*Service" classes in the service layer (excludes interfaces and ServiceBase).
    private static IEnumerable<Type> ConcreteServices =>
        typeof(PetService).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract
                     && t.Namespace == "PetShop.Service.Services"
                     && t.Name.EndsWith("Service"));

    [Fact]
    public void Every_service_inherits_ServiceBase_and_thus_requires_ICurrentUser()
    {
        var offenders = ConcreteServices
            .Where(t => !typeof(ServiceBase).IsAssignableFrom(t))
            .Select(t => t.Name)
            .ToList();

        offenders.Should().BeEmpty(
            "every service must inherit ServiceBase so it receives ICurrentUser");
    }

    [Fact]
    public void ServiceBase_requires_ICurrentUser_in_its_constructor()
    {
        // Guards the guarantee itself: if ICurrentUser is ever dropped from ServiceBase's
        // constructor, the whole "all services have the current user" rule silently breaks.
        var hasCurrentUser = typeof(ServiceBase)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(ICurrentUser)));

        hasCurrentUser.Should().BeTrue(
            "ServiceBase is what propagates ICurrentUser to every service");
    }

    [Fact]
    public void At_least_one_service_is_checked()
    {
        // Cheap guard against the filter silently matching nothing (e.g. namespace renamed),
        // which would make the rule above vacuously pass.
        ConcreteServices.Should().NotBeEmpty();
    }
}
