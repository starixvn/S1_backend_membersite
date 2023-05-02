import areaCode from '../../commJS/areaCode';
export const handleCheckPhoneNo = (value, countryCode)=>{
    let status = 1;
    let message = "";
    let rule = areaCode[countryCode];
    let regx = new RegExp(rule.Pattern, "g");
    if (value != null) {
        if (value.length == 0) {
            status = 2;
            message = 'Please Enter Phone Number';
        } else if (!regx.test(value)) {
            status = 2;
            message = i18n.msg_Wrong;
        } else {
            status = 0;
            message = "success";
        }
    }

    return { status: status, message: message };
}