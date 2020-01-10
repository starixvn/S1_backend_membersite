import {DataField,InputBox} from './InputBox.jsx';
import {number_format} from './common';
import * as Loading from './Loading.jsx';
export class BankModal extends React.Component {
        _clipboard=null;
        constructor(props) {
            super(props);
            let formArr=[];

            let dataField = new DataField("DepositTime");
            dataField.placeholder = i18n.lbl_DepositTime;
            dataField.displayName = i18n.lbl_DepositTime;
            dataField.tooltip = i18n.lbl_DepositTimeToolTip;
            dataField.LabelCommit = <span className="text-accent">{"("+moment().format('Z')+")"}</span>;
            dataField.status = 1;
            dataField.objectType = 6;            
            dataField.handleCheck = this.handleCheck.bind(this);
            let bankInfo = this.props.bankInfo;

            if(moment(bankInfo.depositTime).unix()<0) {
                dataField.Value = moment().format("YYYY-MM-DDTHH:mm:ss");
            } else {
                dataField.Value = moment(bankInfo.depositTime).format("YYYY-MM-DDTHH:mm:ss");
            }
            formArr.push(dataField);

            dataField = new DataField("RefNo");
            dataField.placeholder = i18n.lbl_Refno;
            dataField.displayName = i18n.lbl_Refno;
            dataField.tooltip = i18n.lbl_RefnoToolTip;
            dataField.status = 1;
            dataField.handleCheck = this.handleCheck.bind(this);
            dataField.Value = bankInfo.refId||"";
            formArr.push(dataField);
                        
            this.state={Show: false, UploadDisabled: true, SubmitDisabled: false, formArr: formArr};
            this.filebox = React.createRef();
        }

        async handleCheck(value) {
            let status = 1;
            let message = "";
            
            if(value!=null) {
                if(value.length == 0) {
                    status = 2;
                    message = i18n.lbl_Required;
                } else {
                    status = 3;
                    message = "";
                }
            }

            return {status: status, message: message};
        }        

        componentDidMount() {
            this._clipboard = new ClipboardJS(".copyBtn");
            /*clipboard = new ClipboardJS(this.btn2);
            clipboard = new ClipboardJS(this.btn3);
            clipboard = new ClipboardJS(this.btn4);*/
            this._clipboard.on('success', function(e) {
                //console.log(e);
            });
            this._clipboard.on('error', function(e) {
                //console.log(e);
            });
            this.setState({Show: true});
        }

        componentWillUnmount() {
            this._clipboard.destroy();
        }

        onClose(){
            this.props.onCloseModal();
        }

        onCheckUpload(event) {
            let files = this.filebox.current.files;
            if(files.length >0 ) {
                this.setState({UploadDisabled: false});
            }
            else {
                this.setState({UploadDisabled: true});
            }
        }

        onUploadFile(event) {
            event.preventDefault();
            let files = this.filebox.current.files;
            let bankInfo = this.props.bankInfo;

            if(files.length >0 ) {
                Loading.OnLoading();
                this.setState({UploadDisabled: false});
                let formData = new FormData();                
                formData.append("TransId", bankInfo.transId);                
                formData.append("Receipt", files[0]);
                axios.post('/api/Payment/UploadReceipt', formData, {
                    headers: {
                        'Content-Type': 'multipart/form-data'
                    }
                }).then(function (response) {
                    Loading.UnLoading();
                    let result = response.data;
                    if (result.errorCode == 0) {
                        //this.props.bankInfo.picFileName = files[0].
                        let fullPath = files[0].name;
                        var startIndex = (fullPath.indexOf('\\') >= 0 ? fullPath.lastIndexOf('\\') : fullPath.lastIndexOf('/'));
                        var filename = fullPath.substring(startIndex);
                        if (filename.indexOf('\\') === 0 || filename.indexOf('/') === 0) {
                            filename = filename.substring(1);
                        }
                        this.props.bankInfo.picFileName=filename;
                        this.forceUpdate();
                        alert(i18n.lbl_UploadSuccess);
                    } else {
                        alert(result.message);
                    }
                }.bind(this)).catch(function (error) {
                    Loading.UnLoading();
                    alert(error);
                }.bind(this));          
            } else {
                this.setState({UploadDisabled: true});
            }
        }

        async handleSubmit(event) {
            event.preventDefault();
            Loading.OnLoading();
            
            let data = {};
            let valid = true;
            for(var i=0; i < this.state.formArr.length; i++){
                let dataField = this.state.formArr[i];
                await dataField.onCheck();
                if(valid) {
                    if(dataField.status == 2) {
                        valid = false;
                    }
                }
                data[dataField.name] = dataField.Value;
            }            

            if(!valid) {
                Loading.UnLoading();
                this.setState({SubmitDisabled: true});
            } else {
                this.setState({SubmitDisabled: false});
                let bankInfo = this.props.bankInfo;            
                let postData = {TransId: bankInfo.transId,  DepositTime: moment(data["DepositTime"]).unix(), RefNo: data["RefNo"]};
                axios.post('/api/Payment/SetDepositReceipt', postData)
                .then(function (response) {
                    Loading.UnLoading();
                    let result = response.data;
                    if (result.errorCode == 0) {
                        alert(i18n.lbl_UpdateSuccess);
                        this.onClose();
                    } else {
                        alert(result.message);
                    }
                }.bind(this)).catch(function (error) {
                    Loading.UnLoading();
                    alert(error);
                }.bind(this));
            }
        }

        render() {
            let bankInfo = this.props.bankInfo;
            let Inputs=this.state.formArr.map(dataField => <InputBox key={dataField.name} DataField={dataField}/>);
            return (
            <div className="modal" data-open={this.state.Show}>
                <div className="overlay"></div>
                <div className="modal__dialog">
                    <div className="modal__header">
                        <span className="text">{i18n.lbl_Transaction}</span>
                    </div>
                    <div className="modal__body">
                        <p>{i18n.lbl_TransactionHint}</p>
                        <div className="table table-modal">
                            <div className="tbody">
                                <div className="tr">
                                    <div className="td-left">
                                        <div className="payment">
                                            <div className="payment__img">
                                                <img src={"/common/images/BankLogo/"+bankInfo.bankLogo}/>
                                            </div>
                                            <div className="payment__name">
                                                <span className="text">{bankInfo.bankName}</span>
                                            </div>
                                        </div>
                                    </div>
                                    <div className="td-right">
                                        <a className="btn btn-icon text-primary" target="_blank" href={bankInfo.bankSite}>
                                            <i className="icon icon-new-window"></i>
                                        </a>
                                    </div>
                                </div>
                                <div className="tr">
                                    <div className="td-left">
                                        <span className="text-lowlight">{i18n.lbl_BankAccountName}</span>
                                        <span className="text">{bankInfo.accountName}</span>
                                    </div>
                                    <div className="td-right">
                                        <a className="btn btn-icon text-primary copyBtn" data-clipboard-text={bankInfo.accountName}>
                                            <i className="icon icon-copy"></i>
                                            {/*<div className="tooltip tooltip--top-right" data-open="true">
                                                <span className="text">copy</span>
                                            </div>*/}
                                        </a>
                                    </div>
                                </div>
                                <div className="tr">
                                    <div className="td-left">
                                        <span className="text-lowlight">{i18n.lbl_BankNumber}</span>
                                        <span className="text">{bankInfo.accountNo}</span>
                                    </div>
                                    <div className="td-right">
                                        <button className="btn btn-icon text-primary copyBtn" data-clipboard-text={bankInfo.accountNo}><i className="icon icon-copy"></i></button>
                                    </div>
                                </div>
                                <div className="tr">
                                    <div className="td-left">
                                        <span className="text-lowlight">{i18n.lbl_DepositAmount}</span>
                                        <span className="text">{number_format(parseFloat(bankInfo.amount).toFixed(2))}</span>
                                    </div>
                                    <div className="td-right">
                                        <button className="btn btn-icon text-primary copyBtn" data-clipboard-text={parseFloat(bankInfo.amount)}><i className="icon icon-copy"></i></button>
                                    </div>
                                </div>
                                    {/*<div className="tr">
                                        <div className="td-left">
                                            <span className="text-lowlight">{i18n.lbl_Postscript}</span>
                                            <span className="text-accent">{bankInfo.postScript}</span>
                                        </div>
                                        <div className="td-right">
                                            <button className="btn btn-icon text-primary copyBtn" data-clipboard-text={bankInfo.postScript}><i className="icon icon-copy"></i></button>
                                        </div>
                                            </div>*/}
                            </div>
                                {/*<div className="tfoot">
                                    <div className="tr">
                                        <div className="td-left">
                                            <span className="text">{i18n.lbl_PostscriptHint}</span>
                                        </div>
                                    </div>
                                        </div>*/}
                        </div>
                        <form onSubmit={this.onUploadFile.bind(this)} className="form">
                            <div className="form-group">
                                <label className="form-title">
                                    <span className="text">{i18n.lbl_AttachFile}</span>
                                    <span className="text-accent"></span>
                                </label>
                                <div className="form-content" data-status="">
                                    <div className="textfield">
                                        <input type="file" className="textfield__input" onChange={this.onCheckUpload.bind(this)} ref={this.filebox} accept="image/*"/>
                                        <span className="textfield__bg"></span>
                                        {bankInfo.picFileName!=null? <a className="text-link" target="_blank" href={"/Cashier/DepositReceip.jpg?transId="+bankInfo.transId+"&FileName="+bankInfo.picFileName}><span className="text">{bankInfo.picFileName}</span></a>:null}
                                    </div>
                                    <button className="btn btn-primary" data-disabled={this.state.UploadDisabled}>
                                        <div className="btn__inner">
                                            <span className="text">{i18n.lbl_UploadFile}</span>
                                        </div>
                                    </button>                                     
                                </div>
                            </div>
                        </form>
                        <form className="form" onSubmit={this.handleSubmit.bind(this)}>
                            {Inputs}
                            <input type="submit" style={{display:"none"}}/>
                        </form>
                    </div>
                    <div className="modal__footer">
                        <div className="btn-group">
                            <form className="form" onSubmit={this.handleSubmit.bind(this)}>
                                <button className="btn btn-primary" onClick={this.onClose.bind(this)} type="button">
                                    <div className="btn__inner">
                                        <span className="text">{i18n.lbl_Close}</span>
                                    </div>
                                </button>
                                <button className="btn btn-secondary" data-disabled={false} type="button" onClick={this.handleSubmit.bind(this)}>
                                    <div className="btn__inner">
                                        <span className="text">{i18n.lbl_Submit}</span>
                                    </div>
                                </button>
                            </form>
                        </div>
                    </div>
                </div>
            </div>
            )
        }
    }