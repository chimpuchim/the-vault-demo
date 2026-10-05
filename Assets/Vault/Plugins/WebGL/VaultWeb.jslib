// Browser helpers for the WebGL build: file download (analytics export) and clipboard.
mergeInto(LibraryManager.library, {
  VaultDownloadText: function (namePtr, textPtr) {
    var name = UTF8ToString(namePtr);
    var text = UTF8ToString(textPtr);
    var blob = new Blob([text], { type: "application/json" });
    var a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = name;
    document.body.appendChild(a);
    a.click();
    setTimeout(function () { document.body.removeChild(a); URL.revokeObjectURL(a.href); }, 100);
  },
  VaultCopyText: function (textPtr) {
    var text = UTF8ToString(textPtr);
    if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(text);
  }
});
