using HtmlAgilityPack;

namespace NetTally.Input.Forums.Reading;

internal record PageRequestData(PageRequestInfo RequestInfo, HtmlDocument HtmlDocument);
