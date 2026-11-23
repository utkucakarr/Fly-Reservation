using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace FlyReservation.Api.OpenApi
{
    // Tells OpenAPI clients such as Scalar how the API authenticates: "Authorization: Bearer <JWT>".
    // Without it Scalar has no Authentication box, and every token has to be typed into a header by hand
    public class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
    {
        public const string SchemeName = "Bearer";

        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",      // the client sends "Bearer " + token
                BearerFormat = "JWT",
                Description = "The accessToken from POST /api/auth/login or /api/auth/register"
            };

            return Task.CompletedTask;
        }
    }

    // Asks for the token only where it is needed ([Authorize] on the controller or the action, no [AllowAnonymous]),
    // so the public endpoints (flight search, login) stay open in Scalar too
    public class BearerSecurityRequirementTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            var metadata = context.Description.ActionDescriptor.EndpointMetadata;
            if (!metadata.OfType<IAuthorizeData>().Any() || metadata.OfType<IAllowAnonymous>().Any())
                return Task.CompletedTask;

            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerSecuritySchemeTransformer.SchemeName, context.Document)] = []
            });

            return Task.CompletedTask;
        }
    }
}
