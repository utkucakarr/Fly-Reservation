using FlyReservation.Api.Exceptions;

namespace FlyReservation.Api.Controllers
{
    // A record's version travels as an HTTP ETag: GET sends it, PUT must send it back in If-Match.
    // That is how the server learns which version the client's edit form was built from
    public static class ETags
    {
        public static string From(int version) => $"\"{version}\"";

        public static int ParseIfMatch(string? ifMatch)
        {
            // Without If-Match the update would overwrite whatever changed since the client's read
            if (string.IsNullOrWhiteSpace(ifMatch))
                throw new PreconditionRequiredException(
                    "Send the ETag from the GET response in an If-Match header to update this record.");

            var value = ifMatch.Trim();
            if (value.Length < 3 || value[0] != '"' || value[^1] != '"' || !int.TryParse(value[1..^1], out var version))
                throw new PreconditionFailedException(
                    "If-Match must be the ETag from the GET response, for example \"42\".");

            return version;
        }
    }
}
