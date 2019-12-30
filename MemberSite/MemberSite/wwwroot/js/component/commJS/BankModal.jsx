import {DataField,InputBox} from './InputBox.jsx';
import {number_format} from './common';
import * as Loading from './Loading.jsx';
export class BankModal extends React.Component {
        _clipboard=null;
        constructor(props) {
            super(props);
            let formArr=[];

            let dataField = new DataField("DepositTime");
            dataField.placeholder = "DepositTime";
            dataField.displayName = "DepositTime";
            dataField.tooltip = "DepositTime";
            dataField.status = 1;
            dataField.objectType = 6;            
            dataField.handleCheck = this.handleCheck.bind(this);
            dataField.Value = moment().format("YYYY-MM-DDTHH:mm:ss");
            formArr.push(dataField);

            dataField = new DataField("RefNo");
            dataField.placeholder = "RefNo";
            dataField.displayName = "RefNo";
            dataField.tooltip = "RefNo";
            dataField.status = 1;
            dataField.handleCheck = this.handleCheck.bind(this);
            dataField.Value = "";
            formArr.push(dataField);
                        
            this.state={Show: false, UploadDisabled: false, SubmitDisabled:false, formArr: formArr};
            this.filebox = React.createRef();
        }

        async handleCheck(value) {
            let status = 1;
            let message = "tooltip";
            
            if(value!=null) {
                if(value.length == 0) {
                    status = 2;
                    message = "Req";
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
                        alert("success");
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
                                <div className="tr">
                                    <div className="td-left">
                                        <span className="text-lowlight">{i18n.lbl_Postscript}</span>
                                        <span className="text-accent">{bankInfo.postScript}</span>
                                    </div>
                                    <div className="td-right">
                                        <button className="btn btn-icon text-primary copyBtn" data-clipboard-text={bankInfo.postScript}><i className="icon icon-copy"></i></button>
                                    </div>
                                </div>
                            </div>
                            <div className="tfoot">
                                <div className="tr">
                                    <div className="td-left">
                                        <span className="text">{i18n.lbl_PostscriptHint}</span>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <form onSubmit={this.onUploadFile.bind(this)}>
                            <div className="form-group">
                                <label className="form-title"><span className="text">Attached File</span><span className="text-accent">(*Not required)</span></label>
                                <div className="form-content" data-status="">
                                    <input type="file" name="Receipt" ref={this.filebox} accept="image/*"/>
                                    <button className="btn btn-secondary" data-disabled={this.state.UploadDisabled}>
                                        <div className="btn__inner">
                                            <i className="icon icon-done-circle"></i>
                                            <span className="text">Upload File</span>
                                        </div>
                                    </button>                                     
                                </div>
                            </div>
                        </form>
                        <form className="form" onSubmit={this.handleSubmit.bind(this)}>
                            {Inputs}
                            <div className="btn-group">
                                <button className="btn btn-secondary" data-disabled={this.state.SubmitDisabled} type="submit">
                                    <div className="btn__inner">
                                        <i className="icon icon-done-circle"></i>
                                        <span className="text">Submit</span>
                                    </div>
                                </button>
                            </div>
                        </form>                                             
                        <div className="btn-group">
                            <button className="btn btn-primary" onClick={this.onClose.bind(this)} type="button">
                                <div className="btn__inner">
                                    <span className="text">{i18n.lbl_Close}</span>
                                </div>
                            </button>
                        </div>
                    </div>
                </div>
            </div>
            )
        }
    }