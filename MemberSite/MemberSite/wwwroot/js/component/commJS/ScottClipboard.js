export function copyToClipboard(txt) {
    console.log(txt);
    if (window.clipboardData) {
        console.log("1");
        window.clipboardData.clearData();
        clipboardData.setData("Text", txt);
        alert("複製成功！");
    } else if (navigator.userAgent.indexOf("Opera") != -1) {
        console.log("2");
        window.location = txt;
    } else if (window.netscape) {
        console.log("3");
        try {
            netscape.security.PrivilegeManager.enablePrivilege("UniversalXPConnect");
        } catch (e) {
            alert("被瀏覽器拒絕！\n請在瀏覽器位址列輸入'about:config'並回上一頁\n然後將 'signed.applets.codebase_principal_support'設定為'true'");
        }
        console.log("4");
        let clip = Components.classes['@mozilla.org/widget/clipboard;1'].createInstance(Components.interfaces.nsIClipboard);
        if (!clip)
            return;
        let trans = Components.classes['@mozilla.org/widget/transferable;1'].createInstance(Components.interfaces.nsITransferable);
        if (!trans)
            return;
        console.log("5");
        trans.addDataFlavor("text/unicode");
        let str = Components.classes["@mozilla.org/supports-string;1"].createInstance(Components.interfaces.nsISupportsString);
        let copytext = txt;
        str.data = copytext;
        trans.setTransferData("text/unicode", str, copytext.length * 2);
        let clipid = Components.interfaces.nsIClipboard;
        if (!clip)
            return false;
        clip.setData(trans, null, clipid.kGlobalClipboard);
        alert("複製成功！");
    }
}