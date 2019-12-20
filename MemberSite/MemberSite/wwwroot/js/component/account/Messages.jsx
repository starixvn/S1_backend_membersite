import * as Loading from '../commJS/Loading.jsx';
import {Pagination} from '../commJS/Pagination.jsx';

(function ($, React, ReactDOM, i18n, axios, moment) {
    /*class MessagesBody extends React.Component {
        render() {
            let data = this.props.Data;
            let rows = data.map(function( item ,index ) { 
                let dateMonent = moment(item.createTime);
                return <div className="tr" key={index}>
                        <div className="td-date" data-title="Date" role="mobile-click">
                            <span className="text">{dateMonent.format('l')} ({dateMonent.format('Z')})</span>
                            <i className="icon icon-caret"></i>
                        </div>
                        <div className="td-time" data-title="Time">
                            <span className="text">{dateMonent.format("hh:mm:ss A")}</span>
                        </div>
                        <div className="td-id" data-title="ID">
                            <span className="text"><a className="text-link" onClick={null}>{item.transId}</a></span>
                        </div>
                        <div className="td-method" data-title="Method">
                            <span className="text">{number_format(item.amount.toFixed(2))}</span>
                        </div>
                        <div className="td-amount" data-title="Amount">
                            <span className="text">{number_format(item.chargeFee.toFixed(2))}</span>
                        </div>
                        <div className="td-status" data-title="Status">
                            <span className="text">{item.status}</span>
                        </div>
                </div>;;
                }.bind(this));

                let dateStr = moment(data[0].createTime).format("YYYY/MM/DD");
            return (
                <div className="tbody" data-date={dateStr} data-open="true">
                    {rows}
                </div>
            )
        }
    }*/

    class Messages extends React.Component {
        constructor(props) {
            super(props);
        }

        componentDidMount() {
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
        }

        render() {
            return (
            <div className="page">
                <div className="heading">
                    <span className="text">Message</span>
                </div>
                <div className="message-list">
                    <div className="message-list__header">
                        <label className="checkbox">
                            <input type="checkbox"/>
                            <i className="icon icon-checkbox"></i>
                            <span className="text">Select All</span>
                        </label>
                        <button className="btn btn-icon text-primary">
                            <div className="btn__inner">
                                <i className="icon icon-delete-outline"></i>
                            </div>
                        </button>
                        <button className="btn btn-icon text-primary" data-refresh="false">
                            <div className="btn__inner">
                                <i className="icon icon-refresh"></i>
                                <div className="preloader">
                                    <div className="preloader__spiner">
                                        <div className="preloader__circles">
                                            <div className="preloader__circle-1"></div>
                                            <div className="preloader__circle-2"></div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </button>
                    </div>
                    <div className="message-list__body">
                        <div className="message" data-selected="false" data-read="true" data-open="false">
                            <div className="message__item">
                                <label className="checkbox">
                                    <input type="checkbox"/>
                                    <i className="icon icon-checkbox"></i>
                                </label>
                                <div className="message__title">
                                    <span className="text">Welcome to Nova88!</span>
                                    <span className="text-lowlight">2019/12/04 04:19 PM</span>
                                </div>
                                <div className="btn-area btn-icon">
                                    <i className="icon icon-caret"></i>
                                </div>
                            </div>
                            <div className="message__content">
                                <p>The Company is a leading provider of innovative and exciting interactive gaming
                                    products within the confines of a distinct gaming experience designed for its
                                    members, which is operating under the Nova88 brand.</p>
                            </div>
                        </div>
                        
                    </div>
                </div>
                {null}
            </div>
            )
        }
    }
ReactDOM.render(<Messages/>,document.getElementById('Messages'));
})(jQuery, React, ReactDOM, i18n, axios, moment);