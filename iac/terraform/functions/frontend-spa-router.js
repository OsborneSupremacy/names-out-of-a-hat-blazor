// Sends browser navigation to the SPA without answering every miss with the app.
//
// The distribution used to do this with custom error responses: S3 returned an error for anything
// that was not a file it held, and CloudFront turned that into "/index.html" with a 200. That made
// deep links work, but it applied to files as well as routes, so a missing asset came back as a
// success carrying HTML. A browser asking for /favicon.ico was told 200 and handed a web page.
//
// Rewriting on the way in separates the two cases before the origin ever sees them.
function handler(event) {
    var request = event.request;
    var lastSegment = request.uri.substring(request.uri.lastIndexOf('/') + 1);

    // A dot in the last segment means a file: /favicon.ico, /assets/index-p65XSSj-.js,
    // /logo-square.png. Those go to S3 as they are, so a missing one comes back a genuine 404
    // instead of being papered over.
    if (lastSegment.indexOf('.') !== -1) {
        return request;
    }

    // Everything else is a route: "/", "/auth", "/gift-exchange/{id}". None of the app's routes
    // carry a dot, and the ids in them are GUIDs. Only the origin fetch is rewritten — the address
    // the browser holds is untouched, which is what React Router reads the route out of.
    request.uri = '/index.html';

    return request;
}
