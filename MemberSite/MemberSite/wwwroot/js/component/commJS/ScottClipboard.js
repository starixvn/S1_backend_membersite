/*export function copyToClipboard(txt) {
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
}*/

 let Clipboard = (function(window, document, navigator) {
    var textArea;
    
    function isOS() {
        return navigator.userAgent.match(/ipad|iphone/i);
    }
    
    function createTextArea(text) {
        textArea = document.createElement('textArea');
        //textArea.style.display = "none";
        textArea.value = text;
        document.body.appendChild(textArea);
    }
    
    function selectText() {
        var range,
        selection;
        
        if (isOS()) {
            range = document.createRange();
            range.selectNodeContents(textArea);
            selection = window.getSelection();
            selection.removeAllRanges();
            selection.addRange(range);
            textArea.setSelectionRange(0, 999999);
        } else {
            textArea.select();
        }
    }
    
    function copyToClipboard() {
        document.execCommand("Copy");
        document.body.removeChild(textArea);
    }
    
    let copy = function(text, success, error) {
        try {
            createTextArea(text);
            selectText();
            copyToClipboard();
            console.log(text);
        } catch (e) {
            console.log(e);
            if(error!=null) {
                error(e);
            }
        }

        if(success!=null) {
            success(text);
        }
    };
    
    return {
        copy: copy
    };
})(window, document, navigator);

export {Clipboard};