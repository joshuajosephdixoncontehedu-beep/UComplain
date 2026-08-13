namespace CommunityIncidentReporting.Application.Common.Exceptions;

public class NotFoundException(string entityName, object key)
    : Exception($"{entityName} with id '{key}' was not found.");
