import {DataField, InputBox} from '../commJS/InputBox.jsx';
import {BankModal} from '../commJS/BankModal.jsx';
import * as Loading from '../commJS/Loading.jsx';
import {number_format} from '../commJS/common';

(function ($, React, ReactDOM, i18n, axios, moment, ClipboardJS, window) {
    const DepositStatus = {
        Pending:1,
        Paid :2,
        Approved:3,
        Rejected:4,
        Canceled:5,
        Expired:6,
        OnHold :7
    };

    let onGetDepositInfoRecent = null;

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
            let paymentLogo = null;
            if(this.props.PickBankInfoOff != null){
                let pickBankInfoOff = this.props.PickBankInfoOff;
                paymentLogo = <div className="td-right payment">
                                    <div className="payment__img">
                                        <img src={"/common/images/BankLogo/" + pickBankInfoOff.bankLogo}/>
                                    </div>
                                    <div className="payment__name">
                                        <span className="text">{pickBankInfoOff.bankName}</span>
                                    </div>
                                </div>;
            } else if (this.props.MobilePayGroup != null) {

                let mobilePayGroup = this.props.MobilePayGroup;
                paymentLogo = <div className="td-right payment">
                                    <div className="payment__img">
                                        <img src={"/common/images/MobilePay/" + mobilePayGroup.groupName +".png"}/>
                                    </div>
                                    <div className="payment__name">
                                        <span className="text">{i18n["lbl_"+mobilePayGroup.groupName]}</span>
                                    </div>
                                </div>;
            }

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
                                    {paymentLogo}
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
                            <button className="btn btn-primary" onClick={this.onClose.bind(this)} type="button">
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

                if(item.depositType==3&& (item.status==DepositStatus.Pending||item.status==DepositStatus.OnHold) ) {
                    btnGroup = <div className="btn-group">{item.status==DepositStatus.Pending ? 
                        <button className="btn btn-default" type="button" onClick={this.onCancel.bind(this,item.transId)}>
                            <span className="text">{i18n.lbl_Cancel}</span>
                        </button> : null}
                        <button className="btn btn-secondary" type="button" onClick={this.onShowBankModal.bind(this, {...item.depositRep, ...item.bankInfo, amount: item.amount,postScript: item.postScript,transId: item.transId, refId: item.refId, depositTime: item.depositTime, picFileName: item.picFileName})}>
                            <span className="text">{i18n.lbl_SubmitProof}</span>
                        </button>
                    </div>
                }

                if (item.status==DepositStatus.Pending || item.status==DepositStatus.Approved || item.status==DepositStatus.Rejected || item.status==DepositStatus.Canceled) {
                    statusGroup = <span className="text">{i18n["lbl_DepositStatus" + item.status]}</span>
                } else {
                    statusGroup = <span className="text">{i18n.lbl_DepositStatus8}</span>
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
            onGetDepositInfoRecent = this.GetDepositInfoRecent.bind(this);
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
            this.Dropdown = React.createRef();
        }        
        onPickBankInfoOff(PickBankInfoOff){
            this.props.onPickBankInfoOff(PickBankInfoOff);
            this.setState({dropdown:false});
        }
        
        onDropdown(event) {
            let node = event.target;
            let parentNode = node.parentNode;
            if(node.className != "btn btn-icon btn-form-status" && (parentNode!=undefined && parentNode.className != "btn btn-icon btn-form-status")) {
                let dropdownVal = !this.state.dropdown;
                let $target = $(this.Dropdown.current);

                if(typeof($target.attr("data-open"))=="undefined") {
                    $target.attr("data-open",true);
                    dropdownVal = true;
                }

                this.setState({dropdown: dropdownVal});
            }
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
                        <div className="dropdown dropdown-default" data-open={this.state.dropdown} ref={this.Dropdown}>
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

    class OnlinePayment extends React.Component {
        constructor(props) {
            super(props);
            this.state = { PayId:this.props.PayId, showTodModal:false};
        }       
        
        componentDidMount() {
            this.getOnlineBanking();
        }

        getOnlineBanking() {
            Loading.OnLoading();          
            let data = {payId: this.state.PayId};  
            axios.post('/api/Payment/GetOnlineBankInfoSysList', data)
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
        
        onPickBankInfoOff(PickBankInfoOff){
            this.props.onPickBankInfoOff(PickBankInfoOff);
            this.setState({dropdown:false});
        }
        
        onOnlineBanking(bankInfo) {
            this.props.onOnlineBanking(bankInfo.sysId);
        }

        onDropdown(event) {
            let node = event.target;
            let parentNode = node.parentNode;
            if(node.className != "bt  btn-icon btn-form-status" && (parentNode!=undefined && parentNode.className != "btn btn-icon btn-form-status")) {
                let dropdownVal = !this.state.dropdown;
                let $target = $(this.Dropdown.current);

                if(typeof($target.attr("data-open"))=="undefined") {
                    $target.attr("data-open",true);
                    dropdownVal = true;
                }

                this.setState({dropdown: dropdownVal});
            }
        }

        onDepositOptions() {
            this.props.onDepositOptions();
        }

        render() {
            let onlineBankInfo = this.state.OnlineBankInfo;
 
             let onlineBankInfoList = null;
             if(onlineBankInfo!=null){
                 onlineBankInfoList = onlineBankInfo.map(function( bankInfo ,index ) { 
                 return <a key={bankInfo.bankName} className="payment" onClick={this.onOnlineBanking.bind(this, bankInfo)}>
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
                     <div className="btn-group">
                        <button className="btn btn-primary" type="button" onClick={this.onDepositOptions.bind(this)}>
                            <div className="btn__inner">
                                <span className="text">{i18n.lbl_Back}</span>
                            </div>
                        </button>
                    </div>
                 </div>: null}
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
            var scrolledY = window.scrollY;
            window.scrollTo(0, scrolledY - $($(".header").get(0)).height());
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
                Inputs.push(<BankInfoOffDropdown key={"BankInfoOffList"} BankInfoOffList={this.state.BankInfoOffList} PickBankInfoOff={this.state.PickBankInfoOff} onPickBankInfoOff={this.onPickBankInfoOff.bind(this)} />);
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

    class MobilePayInfoDropdown extends React.Component {
        constructor(props) {
            super(props);
            this.state = { dropdown: false};
            this.Dropdown = React.createRef();
        }        
        onChangeMobilePay(channelId){
            let SelMobilePayInfo=this.props.SelMobilePayInfo;
            if(SelMobilePayInfo.channelId != channelId) {
                this.props.onChangeMobilePay(channelId);
            }
            this.setState({dropdown:false});
        }
        
        onDropdown(event) {
            let node = event.target;
            let parentNode = node.parentNode;
            if(node.className != "btn btn-icon btn-form-status" && (parentNode!=undefined && parentNode.className != "btn btn-icon btn-form-status")) {
                let dropdownVal = !this.state.dropdown;
                let $target = $(this.Dropdown.current);

                if(typeof($target.attr("data-open"))=="undefined") {
                    $target.attr("data-open",true);
                    dropdownVal = true;
                }

                this.setState({dropdown: dropdownVal});
            }
        }

        render() {
            let SelMobilePayInfo=this.props.SelMobilePayInfo;
            let DropdownItem=this.props.MobilePayInfoList.map(function( mobilePayInfo ) { 
                return <div className="dropdown__item" key={mobilePayInfo.channelId} onClick={this.onChangeMobilePay.bind(this, mobilePayInfo.channelId)} data-selected={mobilePayInfo.channelId == SelMobilePayInfo.channelId}>
                    <div className="payment-thumbnail">
                        <img src={"/common/images/MobilePay/" + mobilePayInfo.groupName +".png"} />
                    </div>    
                    <div className="text-group">
                        {mobilePayInfo.minAmount == 0 && mobilePayInfo.maxAmount == 0 ? 
                            <span className="text">{"( "}{i18n.lbl_Limit}{" : "}<span className="text-accent">{i18n.lbl_Unlimited}</span>{" )"}</span>
                        : <span className="text">{i18n.lbl_Limit}{" : "}<span className="text-accent">{number_format(mobilePayInfo.minAmount)}</span>{" ~ "}<span className="text-accent">{number_format(mobilePayInfo.maxAmount)}</span></span>
                        }
                    </div>
                </div>                
                }.bind(this));

            return (
                <div className="form-group">
                    <label className="form-title">{"select channel"}</label>
                    <div className="form-content">
                        <div className="dropdown dropdown-default" data-open={this.state.dropdown} ref={this.Dropdown}>
                            <a className="btn" onClick={this.onDropdown.bind(this)}>
                                <div className="payment-thumbnail">
                                    <img src={"/common/images/MobilePay/" + SelMobilePayInfo.groupName +".png"} />
                                </div>   
                                <div className="text-group">
                                    <span className="text">{i18n["lbl_"+SelMobilePayInfo.groupName]}</span>
                                    {SelMobilePayInfo.minAmount == 0 && SelMobilePayInfo.maxAmount == 0 ? 
                                    <span className="text">{"( "}{i18n.lbl_Limit}{" : "}<span className="text-accent">{i18n.lbl_Unlimited}</span>{" )"}</span>
                                    : <span className="text">{"( "}{i18n.lbl_Limit}{" : "}<span className="text-accent">{number_format(SelMobilePayInfo.minAmount)}</span>{" ~ "}<span className="text-accent">{number_format(SelMobilePayInfo.maxAmount)}</span>{" )"}</span>
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

    class MobilePay extends React.Component {
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
            this.MobilePayForm = React.createRef();

            this.state = { MobilePayGroup:this.props.MobilePayGroup, showTodModal:false, channelId: 0, MobilePayInfoList:null, formArr: formArr, Amount: 0};
        }       

        onClose() {
            this.setState({showTodModal:false});
        }        

        componentDidMount() {
            this.getMobilePayInfoList();
        }

        getMobilePayInfoList() {
            Loading.OnLoading();          
            let data = {GroupId: this.state.MobilePayGroup.groupId};  
            axios.post('/api/Payment/GetMobilePayInfoList', data)
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    let mobilePayInfoList=null;
                    let channelId=0;
                    if(result.data.length>0) {
                        mobilePayInfoList = result.data;
                        channelId=mobilePayInfoList[0].channelId;
                    }
                    this.setState({MobilePayInfoList: mobilePayInfoList, channelId:channelId},function(){
                        this.MobilePayForm.current.scrollIntoView(true);
                        var scrolledY = window.scrollY;
                        window.scrollTo(0, scrolledY - $($(".header").get(0)).height());
                    });
                } else {
                    alert(result.message);
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();
                alert(error);
            }.bind(this));
        }
        
        onChangeMobilePay(channelId) {
            this.setState({channelId: channelId, Amount: 0});
        }

        onDepositOptions() {
            this.props.onDepositOptions();
        }

        async handleCheckAmount(value) {
            let status = 1;
            let message = "";
            let mobilePayInfoList = this.state.MobilePayInfoList;
            let channelId = this.state.channelId;            
            let mobilePayInfo=null;

            for(let i=0;i<mobilePayInfoList.length;i++){
                if(mobilePayInfoList[i].channelId==channelId){
                    mobilePayInfo = mobilePayInfoList[i];
                    break;
                }
            }

            if(isNaN(value)) {
                status = 2;
                message = i18n.lbl_AmountTooltip;
            } else if(value <= 0) {
                status = 2;
                message = i18n.lbl_AmountTooltip;
            } else if((mobilePayInfo.minAmount != 0 && mobilePayInfo.maxAmount != 0) && (value > mobilePayInfo.maxAmount || value < mobilePayInfo.minAmount) ) {
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
            let mobilePayInfoList = this.state.MobilePayInfoList;
            let channelId = this.state.channelId;            
            let mobilePayInfo=null;

            for(let i=0;i<mobilePayInfoList.length;i++){
                if(mobilePayInfoList[i].channelId==channelId){
                    mobilePayInfo = mobilePayInfoList[i];
                    break;
                }
            }

            if(mobilePayInfo.amountRule != 3) {
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
            } else {
                if(this.state.Amount > 0) {
                    this.setState({showTodModal:true});
                } else {
                    alert("Please select an amount");
                }
            }
        }
        
        setAmount(amount) {
            this.setState({Amount:amount});
        }

        onConfirm() {
            this.setState({showTodModal:false});
            let mobilePayInfoList = this.state.MobilePayInfoList;
            let channelId = this.state.channelId;            
            let mobilePayInfo=null;

            for(let i=0;i<mobilePayInfoList.length;i++){
                if(mobilePayInfoList[i].channelId==channelId){
                    mobilePayInfo = mobilePayInfoList[i];
                    break;
                }
            }

            window.open(MobilePayUrl + "?ChannelId=" + mobilePayInfo.channelId +"&Amount="+this.state.Amount);
        }

        render() {
            let mobilePayInfoList = this.state.MobilePayInfoList;
            let mobilePayInfoList1 = this.state.MobilePayInfoList;            
            let InputPanel=null;
            let refMobilePayInfo = null;
             if(mobilePayInfoList!=null){
                let channelId = this.state.channelId;
                 mobilePayInfoList = mobilePayInfoList.map(function( mobilePayInfo ,index ) { 
                 return <a key={mobilePayInfo.channelId} className={"payment" + (mobilePayInfo.channelId == channelId ? " ac":"")} onClick={mobilePayInfo.channelId != channelId ? this.onChangeMobilePay.bind(this, mobilePayInfo.channelId):null}>
                                 <div className="payment__img">
                                     <img src={"/common/images/MobilePay/" + mobilePayInfo.groupName +".png"} />
                                 </div>
                                 <span className="payment__name">{i18n["lbl_"+mobilePayInfo.groupName]}</span>
                                <div className="text-group">
                                    {mobilePayInfo.minAmount == 0 && mobilePayInfo.maxAmount == 0 ? 
                                        <span className="text">{"( "}{i18n.lbl_Limit}{" : "}<span className="text-accent">{i18n.lbl_Unlimited}</span>{" )"}</span>
                                    : <span className="text">{i18n.lbl_Limit}{" : "}<span className="text-accent">{number_format(mobilePayInfo.minAmount)}</span>{" ~ "}<span className="text-accent">{number_format(mobilePayInfo.maxAmount)}</span></span>
                                    }
                                </div>                                 
                             </a>;
                 }.bind(this));

                 if(channelId > 0) {
                    for(let i=0;i<mobilePayInfoList1.length;i++){
                        if(mobilePayInfoList1[i].channelId==channelId){
                            refMobilePayInfo = mobilePayInfoList1[i];
                            break;
                        }
                    }

                    if(refMobilePayInfo.amountRule==3){
                        InputPanel=<div><div className="btn-group btnbg" style={{display: "flex"}}>{refMobilePayInfo.amountList.map(function( amount ) {
                            return <button key={amount} type="button" className={"btn btn-primarys"+ (amount==this.state.Amount ? " ac":"") } onClick={this.setAmount.bind(this, amount)}>
                                        <div className="btn__inner">
                                            <span className="text">{number_format(amount)}</span>
                                        </div>
                                    </button>;
                        }.bind(this))}</div><div className="nutest">{number_format(this.state.Amount)}</div></div>;
                    } else {
                        let dataField = this.state.formArr[0]
                        InputPanel = <InputBox key={dataField.name} DataField={dataField}/>;
                    }
                 }
             }
 
             return (
             <div>
                <div className="heading" ref={this.MobilePayForm}>
                    <span className="text">{i18n.lbl_MobileDeposit}</span>
                </div>
                {mobilePayInfoList != null ? <form className="form MobileDeposit" onSubmit={this.handleSubmit.bind(this)}>
                    {mobilePayInfoList != null ? <MobilePayInfoDropdown MobilePayInfoList={mobilePayInfoList1} SelMobilePayInfo={refMobilePayInfo} onChangeMobilePay={this.onChangeMobilePay.bind(this)}/> : null}
                    {mobilePayInfoList != null ? <div className="payment-list">
                    {mobilePayInfoList}
                    </div>: null}
                    {InputPanel}
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
                {this.state.showTodModal ? <TbdModal onConfirm={this.onConfirm.bind(this)} onClose={this.onClose.bind(this)} MobilePayGroup={this.state.MobilePayGroup} Amount={this.state.Amount} /> : null}
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
            axios.post('/api/Payment/GetOnlineBankInfo', { sysId: this.state.SysId, payId: this.props.PayId})
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    let bankInfoPay=null;
                    if(result.data != null) {
                        bankInfoPay = result.data;

                        this.setState({BankInfoPay: bankInfoPay}, function(){
                            this.onlineBankingForm.current.scrollIntoView(true);
                            var scrolledY = window.scrollY;
                            window.scrollTo(0, scrolledY - $($(".header").get(0)).height());
                        });
                    } else {
                        alert(sysId + "Not Found.");
                        //this.onDepositOptions();
                    }
                } else {
                    alert(result.message);
                    //this.onDepositOptions();
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();
                alert(error);
                //this.onDepositOptions();
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

        onOnlinePayment() {
           this.props.onOnlinePayment(this.props.PayId);
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
                        <button className="btn btn-primary" type="button" onClick={this.onOnlinePayment.bind(this)}>
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

        onOnlinePayment(payId) {
            this.props.onOnlinePayment(payId);
        }

        onWireTransfers() {
            this.props.onWireTransfers();
        }

        onMobilePay(mobilePayGroup) {
            this.props.onMobilePay(mobilePayGroup);
        }

        componentDidMount() {
            this.getLocalBank();
            this.getOnlinePayment();
            this.getMobilePayGroup();
        }

        getMobilePayGroup() {
            Loading.OnLoading();            
            axios.post('/api/Payment/GetMobilePayGroupList')
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    let mobilePayGroupList=null;
                    if(result.data.length>0) {
                        mobilePayGroupList = result.data;
                    }

                    this.setState({MobilePayGroupList: mobilePayGroupList});
                } else {
                    //alert(result.message);
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();
                //alert(error);
            }.bind(this));
        }

        getOnlinePayment() {
            Loading.OnLoading();            
            axios.post('/api/Payment/GetOnlinePaymentList')
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    let onlinePayment=null;
                    if(result.data.length>0) {
                        onlinePayment = result.data;
                    }

                    this.setState({OnlinePayment: onlinePayment});
                } else {
                    //alert(result.message);
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();
                //alert(error);
            }.bind(this));
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

        render() {
            let onlinePayment = this.state.OnlinePayment;

            let onlinePaymentList = null;

            let mobilePayGroupList = this.state.MobilePayGroupList;

            if(onlinePayment!=null){
                onlinePaymentList = onlinePayment.map(function( paymentInfo ,index ) { 
                return <div key={paymentInfo.payId} className="payment" onClick={this.onOnlinePayment.bind(this, paymentInfo.payId)}>
                            <div className="payment__img">
                                <img src={"/common/images/Payment/" + paymentInfo.payName +".png"}  />
                            </div>
                            <span className="payment__name">{paymentInfo.payName}</span>
                            <div className="text-group">
                                {paymentInfo.minAmount == 0 && paymentInfo.maxAmount == 0 ? 
                                    <span className="text">{"( "}{i18n.lbl_Limit}{" : "}<span className="text-accent">{i18n.lbl_Unlimited}</span>{" )"}</span>
                                : <span className="text">{i18n.lbl_Limit}{" : "}<span className="text-accent">{number_format(paymentInfo.minAmount)}</span>{" ~ "}<span className="text-accent">{number_format(paymentInfo.maxAmount)}</span></span>
                                }
                            </div>
                        </div>;
                }.bind(this));
            }

            if(mobilePayGroupList != null){
                mobilePayGroupList = mobilePayGroupList.map(function( mobilePayGroup ,index ) { 
                return <div key={mobilePayGroup.groupId} className="payment" onClick={this.onMobilePay.bind(this, mobilePayGroup)}>
                            <div className="payment__img">
                                <img src={"/common/images/MobilePay/" + mobilePayGroup.groupName +".png"}  />
                            </div>
                            <span className="payment__name">{i18n["lbl_"+mobilePayGroup.groupName]}</span>
                        </div>;
                }.bind(this));
            }

            return (
            <div>
                <div className="heading">
                    <span className="text">{i18n.lbl_Deposit}</span>
                </div>
                <p>{i18n.lbl_DepositOptionHint}</p>
                {onlinePayment != null ? <div className="panel panel-primary panel--payment" data-open="true">
                    <div className="panel__header">
                        <span className="text">{i18n.lbl_OnlineBanking} -</span>
                        <span className="text-accent">{i18n.lbl_CreditTime}: {i18n.lbl_Instant}</span>
                        <i className="icon icon-caret"></i>
                    </div>
                    <div className="panel__body">
                        <div className="payment-list">
                           {onlinePaymentList}
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
                                    <img src="/common/images/Local_Bank.png" />
                                </div>
                                <span className="payment__name">{i18n.lbl_WireTransfers}</span>
                            </a>
                        </div>
                    </div>
                </div> : null}
                {mobilePayGroupList!=null ? <div className="panel panel-primary panel--payment" data-open="true">
                    <div className="panel__header">
                        <span className="text">{i18n.lbl_MobileDeposit} -</span>
                        <span className="text-accent">{i18n.lbl_CreditTime}: {i18n.lbl_Instant}</span>
                        <i className="icon icon-caret"></i>
                    </div>
                    <div className="panel__body">
                        <div className="payment-list">
                            {mobilePayGroupList}
                        </div>
                    </div>
                </div> : null}
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

        onOnlinePayment(payId) {
            //this.state.Select = false;
            this.setState( {"DipositType": 1, PayId: payId, Select: false});
        }

        onMobilePay(mobilePayGroup) {
            this.setState( {"DipositType": 2, MobilePayGroup: mobilePayGroup});
        }

        onOnlineBanking(sysId) {
            this.setState( {"DipositType": 1, SysId: sysId, "Select": true});
        }

        onWireTransfers() {
            this.setState({"DipositType": 3});
        }

        onDepositOptions() {
            this.setState({"DipositType": 0});
        }

        onCloseModal() {
            if(onGetDepositInfoRecent!=null) {
                onGetDepositInfoRecent();
            }            
            this.setState({showModal:false});
        }
        
        onShowBankModal(bankInfo) {
            this.setState({showModal:true, bankInfo: bankInfo,DipositType: 0});
        }
   
        render() {
            let panelObj=[];
            let panelHolder = <DepositOptions key={1} onMobilePay={this.onMobilePay.bind(this)} onWireTransfers={this.onWireTransfers.bind(this)} onShowBankModal={this.onShowBankModal.bind(this)} onOnlinePayment={this.onOnlinePayment.bind(this)} onOnlineBanking={this.onOnlineBanking.bind(this)}/>;
            if (this.state.Select==true) {
                panelHolder = <OnlineBanking key={1} SysId={this.state.SysId} PayId={this.state.PayId} onShowBankModal={this.onShowBankModal.bind(this)} onOnlinePayment={this.onOnlinePayment.bind(this)} />;
            } else if(this.state.DipositType == 1) {
                panelHolder = <OnlinePayment key={1} PayId={this.state.PayId} onDepositOptions={this.onDepositOptions.bind(this)} onShowBankModal={this.onShowBankModal.bind(this)} onOnlineBanking={this.onOnlineBanking.bind(this)}/>;
            } else if(this.state.DipositType == 2) {
                panelHolder = <MobilePay key={1}  MobilePayGroup={this.state.MobilePayGroup} onDepositOptions={this.onDepositOptions.bind(this)} onShowBankModal={this.onShowBankModal.bind(this)} onOnlineBanking={this.onOnlineBanking.bind(this)}/>;                
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
    })(jQuery, React, ReactDOM, i18n, axios, moment, ClipboardJS, window);