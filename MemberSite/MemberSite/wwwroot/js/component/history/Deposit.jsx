import {BankModal} from '../commJS/BankModal.jsx';
import {number_format} from '../commJS/common';
import * as Loading from '../commJS/Loading.jsx';
import {Pagination} from '../commJS/Pagination.jsx';
import {DepositHistory} from '../commJS/DepositHistory.jsx';

(function ($, React, ReactDOM, i18n, axios, moment) {
    class History_Deposit extends React.Component {
        constructor(props) {
            super(props);
            this.state={StartDate:"", EndDate:"", GetData: false};
        }

        onCloseModal() {
            this.setState({showModal:false});
        }
        
        onShowBankModal(bankInfo) {
            this.setState({showModal:true, bankInfo: bankInfo});
        }

        onStartDate(event){
            if(moment(event.target.value).toDate() > moment(this.state.EndDate).toDate() || !moment(this.state.EndDate).isValid()) { 
                this.setState({EndDate:event.target.value, StartDate: event.target.value});
            } else {
                this.setState({StartDate:event.target.value});
            }
        }

        onEndDate(event){
            if(moment(this.state.StartDate).toDate() > moment(event.target.value).toDate() || !moment(this.state.StartDate).isValid()) { 
                this.setState({EndDate:event.target.value,StartDate: event.target.value});
            } else {
                this.setState({EndDate:event.target.value});
            }
        }

        handleSubmit(event) {
            event.preventDefault();
            this.GetDepositInfoHistory(1);
        }

        GetDepositInfoHistory(pageNumber) {
            Loading.OnLoading();
            let endDate = moment(this.state.EndDate).add(1, 'days').unix()-1;
            let data = {StartDate:moment(this.state.StartDate).unix(), EndDate: endDate, PageNumber: pageNumber};
            axios.post('/api/Payment/GetDepositInfoHistory', data)
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    this.setState({PageNumber: result.pageNumber, TotalPages: result.totalPages, DepositInfoHistory: result.data, GetData: true});
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
   
        GetDepositInfoCur() {
            this.GetDepositInfoHistory(this.state.PageNumber);
        }

        render() {
            let modalContaner=null;
            modalContaner = this.state.showModal ? <BankModal bankInfo={this.state.bankInfo} onCloseModal={this.onCloseModal.bind(this)}/> : null;

            return (
            <div>
                <div className="heading">
                    <span className="text">{i18n.lbl_Deposit}</span>
                </div>
                <form onSubmit={this.handleSubmit.bind(this)}>
                    <div className="form-row form-row--bg">
                        <div className="form-group flex-3">
                            <label className="form-title">{i18n.lbl_Date}</label>
                            <div className="form-content form-content--date">
                                <label className="textfield">
                                    <span className="text">{i18n.lbl_From}</span>
                                    <input type="date" className="textfield__input" value={this.state.StartDate} onChange={this.onStartDate.bind(this)} max={moment().format('YYYY-MM-DD')}/>
                                    <i className="icon icon-calendar"></i>
                                    <span className="textfield__bg"></span>
                                </label>
                                <label className="textfield">
                                    <span className="text">{i18n.lbl_To}</span>
                                    <input type="date" className="textfield__input" value={this.state.EndDate} onChange={this.onEndDate.bind(this)} max={moment().format('YYYY-MM-DD')}/>
                                    <i className="icon icon-calendar"></i>
                                    <span className="textfield__bg"></span>
                                </label>
                            </div>
                        </div>
                    </div>
                    <div className="btn-group">
                        <button className="btn btn-secondary" type="submit">
                            <div className="btn__inner">
                                <span className="text">{i18n.lbl_Submit}</span>
                            </div>
                        </button>
                    </div>
                </form>
                {this.state.GetData ?<DepositHistory Data={this.state.DepositInfoHistory} onShowBankModal={this.onShowBankModal.bind(this)} GetDepositInfoCur={this.GetDepositInfoCur.bind(this)}/> : null}
                <Pagination onPaging={this.GetDepositInfoHistory.bind(this)} PageNumber={this.state.PageNumber} TotalPages={this.state.TotalPages}/>
                {modalContaner}
            </div>
            )
        }
    }
    
    ReactDOM.render(<History_Deposit/>,document.getElementById('History'));
    })(jQuery, React, ReactDOM, i18n, axios, moment);