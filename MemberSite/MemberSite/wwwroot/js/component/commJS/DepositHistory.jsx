import {number_format} from '../commJS/common';
class HistoryBody extends React.Component {
    onShowBankModal(bankInfo){
        this.props.onShowBankModal(bankInfo);
    }

    onCancel(transId) {
        if(confirm(i18n.lbl_CancelConfirm)) {
            this.props.onCancel(transId);
        }
    }

    render() {
        const DepositStatus = {
            Pending:1,
            Paid :2,
            Approved:3,
            Rejected:4,
            Canceled:5,
            Expired:6,
            OnHold :7
        };    
        
        let data = this.props.Data;
        let rows = data.map(function( item ,index ) { 
            let dateMonent = moment(item.createTime);
            let btnGroup = null;
            let statusGroup = null;

            if(item.depositType==3 && (item.status==1 || item.status==7)) {
                btnGroup = <div className="btn-group">
                    <button className="btn btn-default" type="button" onClick={this.onCancel.bind(this, item.transId)}>
                        <span className="text">{i18n.lbl_Cancel}</span>
                    </button>
                    <button className="btn btn-secondary" type="button" onClick={this.onShowBankModal.bind(this, {...item.depositRep, ...item.bankInfo, amount: item.amount,postScript: item.postScript,transId: item.transId, refId: item.refId, depositTime: item.depositTime, picFileName: item.picFileName})}>
                        <span className="text">{i18n.lbl_SubmitProof}</span>
                    </button>
                </div>
            }

            if (item.status==DepositStatus.Pending || item.status==DepositStatus.Approved || item.status==DepositStatus.Rejected || item.status==DepositStatus.Canceled) {
                statusGroup = <span className="text">{i18n["lbl_DepositStatus" + item.status]}</span>
            }
            else {
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

export class DepositHistory extends React.Component {
    constructor(props) {
        super(props);
        this.state={refresh: false};
    }

    /*componentDidMount() {
        axios.post('/api/Payment/GetDepositInfoRecent')
        .then(function (response) {
            let result = response.data;
            if (result.errorCode == 0) {
                this.setState({Data: result.data});
            } else {
                alert(result.message);
            }
        }.bind(this)).catch(function (error) {
            alert(error);
        }.bind(this));
    }*/

    onCancel(transId) {
        axios.post('/api/Payment/SetDepositCancel', {TransId: transId})
        .then(function (response) {
            let result = response.data;
            if (result.errorCode == 0) {
                this.GetDepositInfoCur();
            } else {
                alert(result.message);
            }
        }.bind(this)).catch(function (error) {
            alert(error);
        }.bind(this));
    }

    GetDepositInfoCur() {
        this.props.GetDepositInfoCur();
    }

    render() {
        let data=this.props.Data;
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
                return <HistoryBody key={index} Data={item.Data} onShowBankModal={this.props.onShowBankModal} onCancel={this.onCancel.bind(this)}/>;
            }.bind(this));
        }
        let hrefAllTrans = window.hrefAllTrans;
        return (
            data!=null&&data.length > 0 ? <div className="table table-rwd table-primary">
                <div className="caption">
                    <span className="text">{i18n.lbl_TransactionHistory}</span>
                    <a className="btn btn-icon" data-refresh={this.state.refresh} onClick={this.state.refresh?null:this.GetDepositInfoCur.bind(this)}>
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
                        <div className="td-date" data-title={i18n.lbl_Date}>
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
            </div> : <div className="no-data">
                        <span className="text">{i18n.msg_NoData}</span>
                     </div>
        )
    }
}