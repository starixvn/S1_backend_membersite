import {number_format} from '../commJS/common';
const TransferStatus = {0:<span>{i18n.lbl_Success}</span>,
                1:<span>{i18n.lbl_Fail}</span>,
                2:<span>{i18n.lbl_Pending}</span>,
                3:<span>{i18n.lbl_Pending}</span>};  
class HistoryBody extends React.Component {
    render() {
        let data = this.props.Data;
        let rows = data.map(function( item ,index ) { 
            let dateMonent = moment(item.createTime);
            let direction = "";
                if(item.direction==1){
                    direction = <span className="text"><span>{i18n.lbl_MainWallet}</span><i className="icon icon-arrow-right"></i><span>{item.tpName}</span></span>;
                } else {
                    direction = <span className="text"><span>{item.tpName}</span><i className="icon icon-arrow-right"></i><span>{i18n.lbl_MainWallet}</span></span>;
                }

            return <div className="tr" key={index}>
                    <div className="td-date" data-title={i18n.lbl_Date} role="mobile-click">
                        <span className="text">{dateMonent.format('l')} ({dateMonent.format('Z')})</span>
                        <i className="icon icon-caret"></i>
                    </div>
                    <div className="td-time" data-title={i18n.lbl_Time}>
                        <span className="text">{dateMonent.format("hh:mm:ss A")}</span>
                    </div>
                    <div className="td-id" data-title={i18n.lbl_ID}>
                        <span className="text">{item.transId}</span>
                    </div>
                    <div className="td-type" data-title={i18n.lbl_Direction}>
                        {direction}
                    </div>                    
                    <div className="td-amount" data-title={i18n.lbl_Amount}>
                        <span className="text">{number_format(item.amount.toFixed(2))}</span>
                    </div>
                    <div className="td-status" data-title={i18n.lbl_Status}>
                        {TransferStatus[item.status]}
                    </div>
                    {/*<div className="td-method" data-title={i18n.lbl_Memo}>
                        {item.memo}
                    </div>*/}
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

export class TransferHistory extends React.Component {
    constructor(props) {
        super(props);
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
                return <HistoryBody key={index} Data={item.Data} onShowBankModal={this.props.onShowBankModal}/>;
            }.bind(this));
        }
        let hrefAllTrans = window.hrefAllTrans;
        return (
            data!=null&&data.length > 0 ? <div className="table table-rwd table-primary">
                <div className="caption">
                    <span className="text">{i18n.lbl_TransactionHistory}</span>
                    {hrefAllTrans ? <a className="text-link" href={hrefAllTrans}>
                        <span className="text">{i18n.lbl_AllTranscations}</span>
                        <i className="icon icon-arrow-right"></i>
                    </a>: null}
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
                        <div className="td-type" data-title={i18n.lbl_Direction}>
                            <span className=" text">{i18n.lbl_Direction}</span>
                        </div>   
                        <div className="td-amount" data-title={i18n.lbl_Amount}>
                            <span className=" text">{i18n.lbl_Amount}</span>
                        </div>
                        <div className="td-status" data-title={i18n.lbl_Status}>
                            <span className=" text">{i18n.lbl_Status}</span>
                        </div>
                        {/*<div className="td-method" data-title={i18n.lbl_Memo}>
                            <span className=" text">{i18n.lbl_Memo}</span>
                        </div>*/}
                    </div>
                </div>
                {bodyData}
            </div> :<div className="no-data">
                        <span className="text">{i18n.msg_NoData}</span>
                     </div>
        )
    }
}