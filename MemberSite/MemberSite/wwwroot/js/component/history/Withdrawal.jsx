import * as Loading from '../commJS/Loading.jsx';
import {Pagination} from '../commJS/Pagination.jsx';
import {WithdrawalHistory} from '../commJS/WithdrawalHistory.jsx';
import {number_format} from '../commJS/common';
import {QuickPicker, QuickPickDefs} from './component/DateQuickPicker';

(function ($, React, ReactDOM, i18n, axios, moment) {
    class History_Withdrawal extends React.Component {
        constructor(props) {
            super(props);
            this.state={StartDate:moment().format('YYYY-MM-DD'), EndDate:moment().format('YYYY-MM-DD'), GetData: false, QuickPick:QuickPickDefs.Today};
            this.QuickPickOnSelect = this.QuickPickOnSelect.bind(this);
        }

        onCloseModal() {
            this.setState({showModal:false});
        }
        
        onShowBankModal(bankInfo) {
            this.setState({showModal:true, bankInfo: bankInfo,DipositType: 0});
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
            this.GetWithdrawalInfoHistory(1);
        }

        GetWithdrawalInfoHistory(pageNumber) {
            if(this.state.EndDate=="" ||this.state.EndDate=="") { 
                alert(i18n.lbl_DateMissing);
                return;
            }
            Loading.OnLoading();
            let endDate = moment(this.state.EndDate).add(1, 'days').unix()-1;
            let data = {StartDate:moment(this.state.StartDate).unix(), EndDate: endDate, PageNumber: pageNumber};
            axios.post('/api/Payment/GetWithdrawalInfoHistory', data)
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    this.setState({PageNumber: result.pageNumber, TotalPages: result.totalPages, WithdrawalInfoHistory: result.data, GetData: true});
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

        QuickPickOnSelect(v, datacolumn){
            let StartDate = '';
            let EndDate = '';

            switch (v.value) {
                case QuickPickDefs.Today.value:
                    StartDate = moment().format('YYYY-MM-DD');
                    EndDate = moment().format('YYYY-MM-DD');
                    break;
                
                case QuickPickDefs.YesterDay.value:
                    StartDate = moment().add(-1, 'days').format('YYYY-MM-DD');
                    EndDate = moment().add(-1, 'days').format('YYYY-MM-DD');
                    break;

                case QuickPickDefs.LastWeek.value:
                    StartDate = moment().subtract(1, 'weeks').startOf('week').format('YYYY-MM-DD');
                    EndDate = moment().subtract(1, 'weeks').endOf('week').format('YYYY-MM-DD');
                
                    break;

                case QuickPickDefs.ThisWeek.value:
                    StartDate = moment().subtract(1, 'weeks').endOf('week').add(1, 'days').format('YYYY-MM-DD');
                    EndDate = moment().format('YYYY-MM-DD');
                    break;

                case QuickPickDefs.ThisMonth.value:
                    StartDate = moment().subtract(1,'months').endOf('month').add(1, 'days').format('YYYY-MM-DD');
                    EndDate = moment().format('YYYY-MM-DD');
                    break;

                case QuickPickDefs.LastMonth.value:
                    StartDate = moment().subtract(1,'months').startOf('month').format('YYYY-MM-DD');
                    EndDate = moment().subtract(1,'months').endOf('month').format('YYYY-MM-DD');
                    break;

                default:
                    StartDate = stateFilter.StartDate;
                    EndDate = stateFilter.EndDate;
                    break;
            }

            this.setState({
                StartDate:StartDate,
                EndDate:EndDate,
                QuickPick:v
            })
        }
   
        render() {
            let modalContaner=null;

            //modalContaner = this.state.showModal ? <BankModal bankInfo={this.state.bankInfo} onCloseModal={this.onCloseModal.bind(this)}/> : null;

            return (
            <div>
                <div className="heading">
                    <span className="text">{i18n.lbl_Withdrawal}</span>
                </div>
                <form onSubmit={this.handleSubmit.bind(this)}>
                    <div className="form-row form-row--bg">
                        <QuickPicker 
                            QuickPickOnSelect={this.QuickPickOnSelect}
                            QuickPick={this.state.QuickPick}
                        />
                     <div className="form-group flex-3">
                            <label className="form-title">{i18n.lbl_Date}</label>
                            <div className="form-content form-content--date">
                                <label className="textfield">
                                    <span className="text">{i18n.lbl_From}</span>
                                    <input type="date" className="textfield__input" value={this.state.StartDate} onChange={this.onStartDate.bind(this)} max={moment().format('YYYY-MM-DD')} />
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
                {this.state.GetData ?<WithdrawalHistory Data={this.state.WithdrawalInfoHistory}/> : null}
                <Pagination onPaging={this.GetWithdrawalInfoHistory.bind(this)} PageNumber={this.state.PageNumber} TotalPages={this.state.TotalPages}/>
            </div>
            )
        }
    }
    
    ReactDOM.render(<History_Withdrawal/>,document.getElementById('History'));
    })(jQuery, React, ReactDOM, i18n, axios, moment);