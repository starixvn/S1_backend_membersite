import {DataField, InputBox} from '../commJS/InputBox.jsx';
import {BankModal} from '../commJS/BankModal.jsx';
import * as Loading from '../commJS/Loading.jsx';
import {number_format} from '../commJS/common';

(function ($, React, ReactDOM, i18n, axios, moment, ClipboardJS) {
    class TbdModal extends React.Component {
        constructor(props) {
            super(props);
            this.state={Show: false};
        }

        componentDidMount() {
            this.setState({Show: true});
        }

        onClose(){
            this.props.onClose();
        }

        onConfirm(event){
            event.preventDefault();
            this.props.onConfirm();
        }

        render() {
            return (
            <div className="modal" data-open={this.state.Show}>
                <div className="overlay" onClick={this.onClose.bind(this)}></div>
                <div className="modal__dialog">
                    <div className="modal__header">
                        <span className="text">{i18n.lbl_ImportantNote}</span>
                    </div>
                    <form className="modal__body" onSubmit={this.onConfirm.bind(this)}>
                        <p>{i18n.lbl_ConfirmDeposit}</p>
                        <div className="table table-modal">
                            <div className="tbody">
                                <div className="tr">
                                    <div className="td-right payment">
                                        <div className="payment__img">
                                            <img src={"/common/images/BankLogo/" + this.props.PickBankInfoOff.bankLogo}/>
                                        </div>
                                        <div className="payment__name">
                                            <span className="text">{this.props.PickBankInfoOff.bankName}</span>
                                        </div>
                                    </div>
                                </div>                                
                                <div className="tr">
                                    <div className="td-left">
                                        <span className="text-lowlight">{i18n.lbl_DepositAmount}</span>
                                    </div>
                                    <div className="td-right">
                                        <span className="text">{number_format(parseFloat(this.props.Amount).toFixed(2))}</span>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div className="btn-group">
                            <button className="btn btn-primary"  onClick={this.onClose.bind(this)} type="button">
                                <div className="btn__inner">
                                    <span className="text">{i18n.lbl_Cancel}</span>
                                </div>
                            </button>
                            <button className="btn btn-secondary" type="submit">
                                <div className="btn__inner">
                                    <span className="text">{i18n.lbl_Confirm}</span>
                                </div>
                            </button>
                        </div>
                    </form>
                </div>
            </div>
            )
        }
    }
    
    class RecentHistoryBody extends React.Component {
        onShowBankModal(bankInfo){
            this.props.onShowBankModal(bankInfo);
        }

        onCancel(transId){
            if(confirm(i18n.lbl_CancelConfirm)) {
                this.props.onCancel(transId);
            }
        }

        render() {
            let data = this.props.Data;

            let rows = data.map(function( item ,index ) { 
                let dateMonent = moment(item.createTime);
                let btnGroup = null;
                let statusGroup = null;

                if(item.depositType==3 && (item.status==1 || item.status==7)) {
                    btnGroup = <div className="btn-group">
                        <button className="btn btn-default" type="button" onClick={this.onCancel.bind(this,item.transId)}>
                            <span className="text">{i18n.lbl_Cancel}</span>
                        </button>
                        <button className="btn btn-secondary" type="button" onClick={this.onShowBankModal.bind(this, {...item.depositRep, ...item.bankInfo, amount: item.amount,postScript: item.postScript,transId: item.transId})}>
                            <span className="text">{i18n.lbl_SubmitProof}</span>
                        </button>
                    </div>
                }

                if (item.status==5 || item.status==6 || item.status==7 || item.status==99) {
                    statusGroup = <span className="text">{i18n["lbl_DepositStatus" + item.status]}</span>
                }
                else {
                    statusGroup = <span className="text">{i18n.lbl_DepositStatus4}</span>
                }
                                
                return <div className="tr" key={index}>
                    <div className="td-date" data-title="Date" role="mobile-click">
                        <span className="text">{dateMonent.format('l')} ({dateMonent.format('Z')})</span>
                        <i className="icon icon-caret"></i>
                    </div>
                    <div className="td-time" data-title="Time">
                        <span className="text">{dateMonent.format("hh:mm:ss A")}</span>
                    </div>
                    <div className="td-id" data-title="ID">
                        <span className="text">{item.transId}</span>
                    </div>
                    <div className="td-method" data-title="Method">
                        <span className="text">{i18n["lbl_DepositType" + item.depositType]}</span>
                    </div>
                    <div className="td-amount" data-title="Amount">
                        <span className="text">{number_format(item.amount.toFixed(2))}</span>
                    </div>
                    <div className="td-status" data-title="Status">
                        {statusGroup}
                        {btnGroup}
                    </div>
            </div>;
                }.bind(this));

                let dateStr = moment(data[0].createTime).format("YYYY/MM/DD");
            return (
                <div className="tbody" data-date={dateStr} data-open="true">
                    {rows}
                </div>
            )
        }
    }

    class RecentHistory extends React.Component {
        constructor(props) {
            super(props);
            this.state={Data: null, refresh: false};
        }

        componentDidMount() {
            this.GetDepositInfoRecent();
        }

        onCancel(transId) {
            axios.post('/api/Payment/SetDepositCancel', {TransId: transId})
            .then(function (response) {
                let result = response.data;
                if (result.errorCode == 0) {
                    this.GetDepositInfoRecent();
                } else {
                    alert(result.message);
                }
            }.bind(this)).catch(function (error) {
                alert(error);
            }.bind(this));
        }

        GetDepositInfoRecent(){
            this.setState({refresh: true});            
            axios.post('/api/Payment/GetDepositInfoRecent')
            .then(function (response) {
                let result = response.data;
                if (result.errorCode == 0) {
                    this.setState({Data: result.data, refresh: false});
                } else {
                    this.setState({refresh: false});
                    alert(result.message);
                }
            }.bind(this)).catch(function (error) {
                this.setState({refresh: false});
                alert(error);
            }.bind(this));
        }

        render() {
            let data=this.state.Data;
            let bodyData = null;
            if(data!=null){
                let dataContainer=[];
                for(let i=0;i < data.length;i++){
                    let row = data[i];
                    let mDate = moment(row.createTime);
                    let sDate = mDate.format("YYYYMMDD");
                    if(dataContainer.length==0 || dataContainer[dataContainer.length-1].Date != sDate) {
                        dataContainer.push({Date:sDate, Data:[row]});
                    } else {
                        dataContainer[dataContainer.length-1].Data.push(row);
                    }
                }

                bodyData = dataContainer.map(function( item ,index ) { 
                    return <RecentHistoryBody key={index} Data={item.Data} onShowBankModal={this.props.onShowBankModal} onCancel={this.onCancel.bind(this)}/>;
                }.bind(this));
            }

            return (
                <div className="table table-rwd table-primary">
                    <div className="caption">
                        <span className="text">{i18n.lbl_RecentTransactions}</span>
                        <a className="btn btn-icon" data-refresh={this.state.refresh} onClick={this.state.refresh?null:this.GetDepositInfoRecent.bind(this)}>
                            <i className="icon icon-refresh"></i>
                            <div className="preloader">
                                <div className="preloader__spiner">
                                    <div className="preloader__circles">
                                        <div className="preloader__circle-1"></div>
                                        <div className="preloader__circle-2"></div>
                                    </div>
                                </div>
                            </div>
                        </a>                                               
                    </div>
                    <div className="thead" role="desktop-only">
                        <div className="tr">
                            <div className="td-date" data-title="Date">
                                <span className=" text">{i18n.lbl_Date}</span>
                            </div>
                            <div className="td-time" data-title={i18n.lbl_Time}>
                                <span className=" text">{i18n.lbl_Time}</span>
                            </div>
                            <div className="td-id" data-title={i18n.lbl_ID}>
                                <span className=" text">{i18n.lbl_ID}</span>
                            </div>
                            <div className="td-method" data-title={i18n.lbl_Method}>
                                <span className=" text">{i18n.lbl_Method}</span>
                            </div>
                            <div className="td-amount" data-title={i18n.lbl_Amount}>
                                <span className=" text">{i18n.lbl_Amount}</span>
                            </div>
                            <div className="td-status" data-title={i18n.lbl_Status}>
                                <span className=" text">{i18n.lbl_Status}</span>
                            </div>
                        </div>
                    </div>
                    {bodyData}
                    <div className="tfoot">
                        <div className="tr">
                            <div className="td-right">
                                <a className="text-link" href={hrefAllTrans}>
                                    <span className="text">{i18n.lbl_AllTranscations}</span>
                                    <i className="icon icon-arrow-right"></i>
                                </a>
                            </div>
                        </div>
                    </div>
                </div>
            )
        }
    }

    class BankInfoOffDropdown extends React.Component {
        constructor(props) {
            super(props);
            this.state = { dropdown: false};
        }        
        onPickBankInfoOff(PickBankInfoOff){
            this.props.onPickBankInfoOff(PickBankInfoOff);
            this.setState({dropdown:false});
        }
        
        onDropdown(event) {
            let node = event.target;
            let parentNode = node.parentNode;
            if(node.className != "btn btn-icon btn-form-status" && (parentNode!=undefined && parentNode.className != "btn btn-icon btn-form-status")) {
                this.setState({dropdown:!this.state.dropdown}, function() {
                    if(this.state.dropdown){
                        let object = document.getElementsByTagName('body')[0];
                        $(object).on("mousedown", this.onHideDropdown.bind(this));
                    } else {
                        let object = document.getElementsByTagName('body')[0];
                        $(object).off();                
                    }
                });
            }
        }

        onHideDropdown(event) {
            let node = event.target.parentNode;
            let i=1;
            while (node != undefined && i<=3) {
                if(node.className == "dropdown dropdown-default"){
                    return;
                }
    
                node = node.parentNode;
                i++;
            }
    
            this.setState({dropdown:false}, function() {
                let object = document.getElementsByTagName('body')[0];
                $(object).off();
            });
        }

        render() {
            let PickBankInfoOff = this.props.PickBankInfoOff;
            let DropdownItem=this.props.BankInfoOffList.map(function( data ) { 
                return <div className="dropdown__item" key={data.sysId} onClick={this.onPickBankInfoOff.bind(this, data)} data-selected={data.sysId==PickBankInfoOff.sysId}>
                    <div className="payment-thumbnail">
                        <img src={"/common/images/BankLogo/" + data.bankLogo}/>
                    </div>    
                    <div className="text-group">
                        <span className="text">{data.bankName}</span>
                        {data.minAmount == 0 && data.maxAmount == 0 ? 
                            <span className="text">{"( "}{i18n.lbl_Limit}{" : "}<span className="text-accent">{i18n.lbl_Unlimited}</span>{" )"}</span>
                        : <span className="text">{"( "}{i18n.lbl_Limit}{" : "}<span className="text-accent">{number_format(data.minAmount)}</span>{" ~ "}<span className="text-accent">{number_format(data.maxAmount)}</span>{" )"}</span>
                        }
                    </div>
                </div>                
                }.bind(this));

            return (
                <div className="form-group">
                    <label className="form-title">{i18n.lbl_TransferTo}</label>
                    <div className="form-content">
                        <div className="dropdown dropdown-default" data-open={this.state.dropdown}>
                            <a className="btn" onClick={this.onDropdown.bind(this)}>
                                <div className="payment-thumbnail">
                                    <img src={"/common/images/BankLogo/" + PickBankInfoOff.bankLogo} />
                                </div>   
                                <div className="text-group">
                                    <span className="text">{PickBankInfoOff.bankName}</span>
                                    {PickBankInfoOff.minAmount == 0 && PickBankInfoOff.maxAmount == 0 ? 
                                    <span className="text">{"( "}{i18n.lbl_Limit}{" : "}<span className="text-accent">{i18n.lbl_Unlimited}</span>{" )"}</span>
                                    : <span className="text">{"( "}{i18n.lbl_Limit}{" : "}<span className="text-accent">{number_format(PickBankInfoOff.minAmount)}</span>{" ~ "}<span className="text-accent">{number_format(PickBankInfoOff.maxAmount)}</span>{" )"}</span>
                                    }
                                </div>
                                <i className="icon icon-caret"></i>
                            </a>
                            <div className="dropdown__container">
                                {DropdownItem}
                            </div>
                        </div>
                    </div>
                </div>
            )
        }
    }
    
    class WireTransfers extends React.Component {
        constructor(props) {
            super(props);

            let formArr=[];

            let amountDataField = new DataField("Amount");
            amountDataField.placeholder=i18n.lbl_Amount;
            amountDataField.displayName=i18n.lbl_Amount;
            amountDataField.tooltip=i18n.lbl_AmountTooltip;
            amountDataField.status=1;
            amountDataField.objectType=5;
            amountDataField.handleCheck = this.handleCheckAmount.bind(this);
            amountDataField.Value="";
            formArr.push(amountDataField);
            this.wireTransfersForm = React.createRef();
            this.state = { formArr: formArr, BankInfoOffList: null, PickBankInfoOff:null, showTodModal:false};
        }

        componentDidMount() {
            this.wireTransfersForm.current.scrollIntoView(true);
            Loading.OnLoading();            
            axios.post('/api/Payment/GetBankInfoOffList')
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    let BankInfoOff=null;
                    if(result.data.length>0) {
                        BankInfoOff = result.data[0];
                    }

                    this.setState({BankInfoOffList: result.data, PickBankInfoOff: BankInfoOff});
                } else {
                    alert(result.message);
                    this.onDepositOptions();
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();
                alert(error);
                this.onDepositOptions();
            }.bind(this));
        }

        async onPickBankInfoOff(BankInfoOff){
            this.setState({PickBankInfoOff: BankInfoOff},async function(){
                for(var i=0; i < this.state.formArr.length; i++){
                    let dataField = this.state.formArr[i];
                    await dataField.onCheck();
                }

                this.forceUpdate();
            }.bind(this));
        }

        async handleCheckAmount(value) {
            let status = 1;
            let message = "";
            let PickBankInfoOff = this.state.PickBankInfoOff;
            if(value!=null) {
                if(value <= 0) {
                    status = 2;
                    message = i18n.lbl_AmountTooltip;
                } else if((PickBankInfoOff.minAmount != 0 && PickBankInfoOff.maxAmount != 0) && (value > PickBankInfoOff.maxAmount || value < PickBankInfoOff.minAmount) ) {
                    status = 2;
                    message = number_format(value) + " "+ i18n.lbl_NotinLimit;
                } else {
                    status = 3;
                    message = "";
                }
            }
    
            return {status: status, message: message};
        }

        async handleSubmit(event) {
            event.preventDefault();
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
                this.forceUpdate();
            } else {
                this.setState({showTodModal:true, Amount: parseFloat(data["Amount"])});
            }
        }
       
        onDepositOptions() {
            this.props.onDepositOptions();
        }

        onClose() {
            this.setState({showTodModal:false});
        }

        onConfirm() {
            this.setState({showTodModal:false});
            Loading.OnLoading();
            let data = {sysId: this.state.PickBankInfoOff.sysId, amount:this.state.Amount};
            axios.post('/api/Payment/DepositReqWireTransfer', data)
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    let bankInfo = result.data;
                    bankInfo.amount=this.state.Amount;
                    this.props.onShowBankModal(result.data);
                } else {
                    alert(result.message);
                    this.setState({showTodModal:true});
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();
                alert(error);
                this.setState({showTodModal:true});                    
            }.bind(this));
        }

        render() {
            let Inputs=this.state.formArr.map(dataField => <InputBox key={dataField.name} DataField={dataField}/>);
            let BankInfoOffList = this.state.BankInfoOffList;
            if(BankInfoOffList != null && BankInfoOffList.length > 0) {
                Inputs.push(<BankInfoOffDropdown key={"."} BankInfoOffList={this.state.BankInfoOffList} PickBankInfoOff={this.state.PickBankInfoOff} onPickBankInfoOff={this.onPickBankInfoOff.bind(this)} />);
            }
            //let modalContaner = null;

            return (
            <div>
                <div className="heading" ref={this.wireTransfersForm}>
                    <span className="text">{i18n.lbl_WireTransfers}</span>
                </div>
                <form className="form" onSubmit={this.handleSubmit.bind(this)}>
                    {Inputs}
                    <div className="btn-group">
                        <button className="btn btn-primary" type="button" onClick={this.onDepositOptions.bind(this)}>
                            <div className="btn__inner">
                                <span className="text">{i18n.lbl_Back}</span>
                            </div>
                        </button>
                        <button className="btn btn-secondary" type="submit">
                            <div className="btn__inner">
                                <span className="text">{i18n.lbl_Submit}</span>
                            </div>
                        </button>
                    </div>
                </form>
                {this.state.showTodModal ? <TbdModal onConfirm={this.onConfirm.bind(this)} onClose={this.onClose.bind(this)} PickBankInfoOff={this.state.PickBankInfoOff} Amount={this.state.Amount}/> : null}
            </div>  
            )
        }
    }

    class OnlineBanking extends React.Component {
        constructor(props) {
            super(props);

            let formArr=[];

            let amountDataField = new DataField("Amount");
            amountDataField.placeholder=i18n.lbl_Amount;
            amountDataField.displayName=i18n.lbl_Amount;
            amountDataField.tooltip=i18n.lbl_AmountTooltip;
            amountDataField.status=1;
            amountDataField.objectType=5;
            amountDataField.handleCheck = this.handleCheckAmount.bind(this);
            amountDataField.Value="";
            formArr.push(amountDataField);
            this.onlineBankingForm = React.createRef();
            this.state = { formArr: formArr, BankInfoOffList: null, SysId:this.props.SysId, showTodModal:false};
        }

        componentDidMount() {
            Loading.OnLoading();            
            axios.post('/api/Payment/GetOnlineBankInfo', { sysId: this.state.SysId})
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    let bankInfoPay=null;
                    if(result.data != null) {
                        bankInfoPay = result.data;
                        if(bankInfoPay!=null){
                            this.onlineBankingForm.current.scrollIntoView(true);
                        }
                        this.setState({BankInfoPay: bankInfoPay});
                    } else {
                        alert(sysId + "Not Found.");
                        this.onDepositOptions();                        
                    }
                } else {
                    alert(result.message);
                    this.onDepositOptions();
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();
                alert(error);
                this.onDepositOptions();
            }.bind(this));
        }

        async handleCheckAmount(value) {
            let status = 1;
            let message = "";
            let BankInfoPay = this.state.BankInfoPay;
            if(isNaN(value)) {
                status = 2;
                message = i18n.lbl_AmountTooltip;
            } else if(value <= 0) {
                status = 2;
                message = i18n.lbl_AmountTooltip;
            } else if((BankInfoPay.minAmount != 0 && BankInfoPay.maxAmount != 0) && (value > BankInfoPay.maxAmount || value < BankInfoPay.minAmount) ) {
                status = 2;
                message = number_format(value) + " "+ i18n.lbl_NotinLimit;
            } else {
                status = 3;
                message = "";
            }
    
            return {status: status, message: message};
        }

        async handleSubmit(event) {
            event.preventDefault();
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
                this.forceUpdate();
            } else {
                this.setState({showTodModal:true, Amount: parseFloat(data["Amount"])});
            }
        }
       
        onDepositOptions() {
            this.props.onDepositOptions();
        }

        onClose() {
            this.setState({showTodModal:false});
        }

        onConfirm() {
            this.setState({showTodModal:false});
            let bankInfoPay = this.state.BankInfoPay;
            window.open(OnlineBankingUrl + "?sysId="+ bankInfoPay.sysId + "&payId=" + bankInfoPay.payId + "&ChannelId="+ this.state.BankInfoPay.channelId +"&Amount="+this.state.Amount);
        }

        render() {
            let BankInfoPay = this.state.BankInfoPay;

            let Inputs=this.state.formArr.map(function( dataField ) { 
                dataField.LabelCommit = BankInfoPay !=null ? <span className="text">{"( "}{i18n.lbl_Limit}{" : "}<span className="text-accent">{number_format(BankInfoPay.minAmount)}</span>{" ~ "}<span className="text-accent">{number_format(BankInfoPay.maxAmount)}</span>{" )"}</span> : null;
                return <InputBox key={dataField.name} DataField={dataField}/>;
            });
            //let modalContaner = null;

            return (
            <div>
                <div className="heading" ref={this.onlineBankingForm}>
                    <span className="text">{i18n.lbl_OnlineBanking}</span>
                </div>
                {BankInfoPay != null ? <form className="form" onSubmit={this.handleSubmit.bind(this)}>
                    <div className="payment">
                        <div className="payment__img">
                            <img src={"/common/images/BankLogo/" + BankInfoPay.bankLogo}/>
                        </div>
                        <span className="payment__name">{BankInfoPay.bankName}</span>
                    </div>
                    {Inputs}
                    <div className="btn-group">
                        <button className="btn btn-primary" type="button" onClick={this.onDepositOptions.bind(this)}>
                            <div className="btn__inner">
                                <span className="text">{i18n.lbl_Back}</span>
                            </div>
                        </button>
                        <button className="btn btn-secondary" type="submit">
                            <div className="btn__inner">
                                <span className="text">{i18n.lbl_Submit}</span>
                            </div>
                        </button>
                    </div>
                </form> : null}
                {this.state.showTodModal && BankInfoPay!=null ? <TbdModal onConfirm={this.onConfirm.bind(this)} onClose={this.onClose.bind(this)} PickBankInfoOff={BankInfoPay} Amount={this.state.Amount} /> : null}
            </div>
            )
        }
    }

    class DepositOptions extends React.Component {
        constructor(props) {
            super(props);
            
            this.state={LocalBank: false};
        }

        onWireTransfers() {
            this.props.onWireTransfers();
        }

        componentDidMount() {
            this.getLocalBank();
            this.getOnlineBanking();
        }

        getLocalBank() {
            Loading.OnLoading();            
            axios.post('/api/Payment/GetBankInfoOffList')
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    let localBank=false;
                    if(result.data.length>0) {
                        localBank = true;
                    }

                    this.setState({LocalBank: localBank});
                } else {
                    //alert(result.message);
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();
                //alert(error);
            }.bind(this));
        }

        onOnlineBanking(bankInfo) {
            this.props.onOnlineBanking(bankInfo.sysId);
        }

        getOnlineBanking() {
            Loading.OnLoading();            
            axios.post('/api/Payment/GetOnlineBankInfoSysList')
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    let onlineBankInfo=null;
                    if(result.data.length>0) {
                        onlineBankInfo = result.data;
                    }

                    this.setState({OnlineBankInfo: onlineBankInfo});
                } else {
                    //alert(result.message);
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();
                //alert(error);
            }.bind(this));
        }

        render() {
            let onlineBankInfo = this.state.OnlineBankInfo;

            let onlineBankInfoList = null;

            if(onlineBankInfo!=null){
                onlineBankInfoList = onlineBankInfo.map(function( bankInfo ,index ) { 
                return <a key={bankInfo.sysId} className="payment" onClick={this.onOnlineBanking.bind(this, bankInfo)}>
                                <div className="payment__img">
                                    <img src={"/common/images/BankLogo/" + bankInfo.bankLogo}/>
                                </div>
                                <span className="payment__name">{bankInfo.bankName}</span>
                            </a>;
                }.bind(this));
            }

            return (
            <div>
                <div className="heading">
                    <span className="text">{i18n.lbl_Deposit}</span>
                </div>
                <p>{i18n.lbl_DepositOptionHint}</p>
                {onlineBankInfo != null ? <div className="panel panel-primary panel--payment" data-open="true">
                    <div className="panel__header">
                        <span className="text">{i18n.lbl_OnlineBanking} -</span>
                        <span className="text-accent">{i18n.lbl_CreditTime}: {i18n.lbl_Instant}</span>
                        <i className="icon icon-caret"></i>
                    </div>
                    <div className="panel__body">
                        <div className="payment-list">
                           {onlineBankInfoList}
                        </div>
                    </div>
                </div>: null}
                {this.state.LocalBank ? <div className="panel panel-primary panel--payment" data-open={true}>
                    <div className="panel__header">
                        <span className="text">{i18n.lbl_WireTransfers} -</span>
                        <span className="text-accent">{i18n.lbl_CreditTime}: {i18n.lbl_5mins}</span>
                        <i className="icon icon-caret"></i>
                    </div>
                    <div className="panel__body">
                        <div className="payment-list">
                            <a className="payment" onClick={this.onWireTransfers.bind(this)}>
                                <div className="payment__img">
                                    <img src="/common/images/wire-transfer.png"/>
                                </div>
                                <span className="payment__name">{i18n.lbl_WireTransfers}</span>
                            </a>
                        </div>
                    </div>
                </div> : null}
                {/*<div className="panel panel-primary panel--payment" data-open="true">
                    <div className="panel__header">
                        <span className="text">{i18n.lbl_MobileDeposit} -</span>
                        <span className="text-accent">{i18n.lbl_CreditTime}: {i18n.lbl_Instant}</span>
                        <i className="icon icon-caret"></i>
                    </div>
                    <div className="panel__body">
                        <div className="payment">
                            <a className="payment" href="#">
                                <div className="payment__img">
                                    <img src="../../SiteSkin/Nova88/common/images/payment/alipay.png"/>
                                </div>
                                <span className="payment__name">Alipay</span>
                            </a>
                            <a className="payment__item" href="#">
                                <div className="payment__img">
                                    <img src="../../SiteSkin/Nova88/common/images/payment/wechat-pay.png"/>
                                </div>
                                <span className="payment__text">WeChat Pay</span>
                            </a>
                        </div>
                    </div>
                </div>*/}
                <RecentHistory onShowBankModal={this.props.onShowBankModal}/>
            </div>                
            )
        }
    }

    class DepositPanel extends React.Component {
        constructor(props) {
            super(props);
            this.state={"DipositType": 0, showModal: false };
        }

        onWireTransfers() {
            this.setState({"DipositType": 3});
        }

        onOnlineBanking(sysId) {
            this.setState({"DipositType": 1, SysId: sysId});
        }

        onDepositOptions() {
            this.setState({"DipositType": 0});
        }

        onCloseModal() {
            this.setState({showModal:false});
        }
        
        onShowBankModal(bankInfo) {
            this.setState({showModal:true, bankInfo: bankInfo,DipositType: 0});
        }
   
        render() {
            let panelObj=[];
            let panelHolder = <DepositOptions key={1} onWireTransfers={this.onWireTransfers.bind(this)} onShowBankModal={this.onShowBankModal.bind(this)} onOnlineBanking={this.onOnlineBanking.bind(this)}/>;
            if(this.state.DipositType == 1) {
                panelHolder = <OnlineBanking key={1} SysId={this.state.SysId} onDepositOptions={this.onDepositOptions.bind(this)} onShowBankModal={this.onShowBankModal.bind(this)}/>;
            } else if(this.state.DipositType == 3) {
                panelHolder = <WireTransfers key={1} onDepositOptions={this.onDepositOptions.bind(this)} onShowBankModal={this.onShowBankModal.bind(this)}/>;
            } 

            panelObj.push(panelHolder);
            let modalContaner=null;

            modalContaner = this.state.showModal ? <BankModal key={2} bankInfo={this.state.bankInfo} onCloseModal={this.onCloseModal.bind(this)}/> : null;
            panelObj.push(modalContaner);
            return (
                panelObj
            )
        }
    }
    
    ReactDOM.render(<DepositPanel/>,document.getElementById('DepositPanel'));
    })(jQuery, React, ReactDOM, i18n, axios, moment, ClipboardJS);