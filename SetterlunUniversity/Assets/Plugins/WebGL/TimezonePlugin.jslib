// TimezonePlugin.jslib
mergeInto(LibraryManager.library, {

    GetUserTimezone: function () {
        var timezone = Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC";
        
        // Convert to C# string
        var bufferSize = lengthBytesUTF8(timezone) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(timezone, buffer, bufferSize);
        return buffer;
    }
});