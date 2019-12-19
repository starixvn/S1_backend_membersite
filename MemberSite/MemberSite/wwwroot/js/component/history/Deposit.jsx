import * as Loading from '../commJS/Loading.jsx';
import {Pagination} from '../commJS/Pagination.jsx';
import {DepoistHistory} from '../commJS/DepoistHistory.jsx';
import {number_format} from '../commJS/common';

(function ($, React, ReactDOM, i18n, axios, moment) {
    class DepositHistory extends React.Component {
        constructor(props) {
            super(props);
            this.state={StartDate:"", EndDate:"", GetData: false};
        }

        onCloseModal() {
            this.setState({showModal:false});
        }
        
        onShowBankModal(bankInfo) {
            this.setState({showModal:true, bankInfo: bankInfo,DipositType: 0});
        }

        onStartDate(event){
            this.setState({StartDate:event.target.value});
            
            if(moment(event.target.value).toDate() > moment(this.state.EndDate).toDate()) { 
                this.setState({EndDate:event.target.value, StartDate: event.target.value});
            } else {
                this.setState({EndDate:event.target.value});
            }
        }

        onEndDate(event){
            if(moment(this.state.StartDate).toDate() > moment(event.target.value).toDate()) { 
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
   
        render() {
            let modalContaner=null;

            //modalContaner = this.state.showModal ? <BankModal bankInfo={this.state.bankInfo} onCloseModal={this.onCloseModal.bind(this)}/> : null;

            return (
            <div className="page">
                <div className="heading" role="desktop-only">
                    <span className="text">Deposit</span>
                </div>
                <form onSubmit={this.handleSubmit.bind(this)}>
                    <div className="form-row form-row--bg">
                        <div className="form-group flex-2">
                            <label className="form-title">Date</label>
                            <div className="form-content">
                                <div className="textfield">
                                    <i className="icon icon-calendar"></i>
                                    <input type="date" className="textfield__input" value={this.state.StartDate} onChange={this.onStartDate.bind(this)}/>
                                    <span className="text">to</span>
                                    <i className="icon icon-calendar"></i>
                                    <input type="date" className="textfield__input" value={this.state.EndDate} onChange={this.onEndDate.bind(this)}/>
                                    <span className="textfield__bg"></span>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div className="btn-group">
                        <button className="btn btn-secondary" type="submit">
                            <div className="btn__inner">
                                <span className="text">Submit</span>
                            </div>
                        </button>
                    </div>
                </form>
                {this.state.GetData ?<DepoistHistory Data={this.state.DepositInfoHistory}/> : null}
                <Pagination onPaging={this.GetDepositInfoHistory.bind(this)} PageNumber={this.state.PageNumber} TotalPages={this.state.TotalPages}/>
            </div>
            )
        }
    }
    
    ReactDOM.render(<DepositHistory/>,document.getElementById('History'));
    })(jQuery, React, ReactDOM, i18n, axios, moment);