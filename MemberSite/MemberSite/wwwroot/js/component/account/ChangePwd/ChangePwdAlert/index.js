import {Modal} from './components/modal';
import {InputBox, InputBoxStatus} from './components/inputBox';

(function ($, React, ReactDOM, i18n, axios) {

    const dataStatus = {
        'info':'info',
        'valid':'valid',
        'error':'error'
    }

    const handleCheckUserpwd = (value)=>{
        let status = 1;
        let message = "tooltip";
        let regx = /^[a-zA-Z0-9@$*!%#?&]*$/;
        if(value!=null) {
            if(value.length == 0) {
                status = 2;
                message = i18n.msg_PasswordReq;
            } else if(value.length < 6 || value.length > 18) {
                status = 2;
                message = i18n.msg_PasswordLength;
            } else if(!regx.test(value)) {
                status = 2;
                message = i18n.msg_PasswordValidate;
            } else {
                status = 3;
                message = "";
            }
        }

        return {status: status, message: message};
    }

    const Panel = (props)=>{
        const [stateModal, updateModal] = React.useState({
            isOpen:true
        });

        const CloseModal = ()=>{
            updateModal({
                isOpen:false
            });

            if(DepositAlert === 1){
                $("#DepositAlet").attr("data-open", true);
            }
        };

        const [stateData, updateData] = React.useState({
            oldpwd:{
                value:'',
                type:InputBoxStatus.password,
                errorMsg:'',
                showtips:false,
                dataStatus:dataStatus.info
            },
            newpwd:{
                value:'',
                type:InputBoxStatus.password,
                errorMsg:'',
                showtips:false,
                dataStatus:dataStatus.info
            }
        });

        const EditField = (v, datacolumn)=>{
            let check = handleCheckUserpwd(v);
            updateData({
                ...stateData,
                [datacolumn]:{
                    ...stateData[datacolumn],
                    value:v,
                    showtips:false,
                    errorMsg: check.message,
                    dataStatus: check.status === 3 ? dataStatus.valid : dataStatus.error
                }
            });
        }

        const UpdatePwd = ()=>{
            let check = true;
            Object.keys(stateData).forEach(key => {
                let result = handleCheckUserpwd(stateData[key].value);
                if(result.status !== 3){
                    stateData[key].errorMsg = result.message;
                    stateData[key].dataStatus = dataStatus.error;
                    check = false;
                }
            });

            if(check){
                axios.post('/api/Account/ChangePassword', {OldPassword: stateData.oldpwd.value, NewPassword: stateData.newpwd.value})
                .then(function (response) {
                    let result = response.data;
                    if (result.errorCode == 0) {
                        alert(result.message);
                        updateModal({
                            isOpen:false
                        });
                        if(DepositAlert===1){
                            $("#DepositAlet").attr("data-open", true);
                        }
                    } else {
                        if(result.errorCode===-3){
                            updateData({
                                ...stateData,
                                oldpwd:{
                                    ...stateData.oldpwd,
                                    errorMsg:result.message,
                                    dataStatus:dataStatus.error
                                }
                            });
                        }else if(result.errorCode===-1){
                            updateData({
                                ...stateData,
                                newpwd:{
                                    ...stateData.oldpwd,
                                    errorMsg:result.message,
                                    dataStatus:dataStatus.error
                                }
                            });
                        }else{
                            alert(result.message);
                        }
                    }
                }).catch(function (error) {
                    Loading.UnLoading();
                    if (error.response.status == 401) {
                        window.location.replace(window.location.href);
                    } else {                
                        alert(error);
                    }
                });
            }else{
                updateData({
                    ...stateData
                })
            }
        }

        const HandleVisible = (datacolumn)=>{
            updateData({
                ...stateData,
                [datacolumn]:{
                    ...stateData[datacolumn],
                    type:stateData[datacolumn].type === InputBoxStatus.password ? InputBoxStatus.text : InputBoxStatus.password
                }
            });
        }

        const HandleTips = (datacolumn)=>{
            updateData({
                ...stateData,
                [datacolumn]:{
                    ...stateData[datacolumn],
                    showtips:!stateData[datacolumn].showtips
                }
            });
        }

        return(
            <Modal
                isOpen={stateModal.isOpen}
                title={i18n.lbl_ChangePassword}
                content={<ChangePwdContent data={stateData} EditField={EditField} HandleVisible={HandleVisible} HandleTips={HandleTips}/>}
                btnBar={<BtnBar CloseModal={CloseModal} UpdatePwd={UpdatePwd}/>}
            />
        );
    }

    const ChangePwdContent = (props)=>{
        const columns = [
            {datacolumn: 'oldpwd', columnTitle:i18n.lbl_OldPassword, placeholder:i18n.lbl_OldPassword, tooltip:i18n.lbl_PasswordTooltip, autoComplete:false},
            {datacolumn: 'newpwd', columnTitle:i18n.lbl_NewPassword, placeholder:i18n.lbl_NewPassword, tooltip:i18n.lbl_PasswordTooltip, autoComplete:false},
        ];
        
        let rows = columns.map(col=>{
            let dataItem = props.data[col.datacolumn];
            let visibility = dataItem.type === InputBoxStatus.text;
            let tipsMsg = dataItem.dataStatus === dataStatus.error ? dataItem.errorMsg : dataItem.showtips ? col.tooltip : '';
            return (
                <InputBox
                    key={`changePwd-${col.datacolumn}`}
                    datacolumn={col.datacolumn}
                    columnTitle={col.columnTitle}
                    value={dataItem.value}
                    onChange={props.EditField}
                    type={dataItem.type}
                    placeholder={col.placeholder}
                    visibility={visibility}
                    tipsMsg={tipsMsg}
                    autoComplete={col.autoComplete}
                    HandleVisible={props.HandleVisible.bind(null, col.datacolumn)}
                    HandleTips={props.HandleTips.bind(null, col.datacolumn)}
                    dataStatus={dataItem.dataStatus}
                />
            )
        })
        return(
            <form className="form" id="pwdForm">
                {rows}
            </form>
        )
    }

    const BtnBar = (props)=>{
        return(
            <div className="btn-group">
                <button className="btn btn-primary" onClick={props.CloseModal} type="button">
                    <span className="text">{i18n.lbl_Later}</span>
                </button>
                {/* <button type="submit" form="pwdForm" className="btn btn-secondary" onClick={props.UpdatePwd}>
                    <span className="text">{i18n.lbl_Update}</span>
                </button> */}
                <button type="button" className="btn btn-secondary" onClick={props.UpdatePwd}>
                    <span className="text">{i18n.lbl_Update}</span>
                </button>
            </div>
        )
    }

    ReactDOM.render(<Panel />,document.getElementById('ResetPwdAlert'));
})(jQuery, React, ReactDOM, i18n, axios);