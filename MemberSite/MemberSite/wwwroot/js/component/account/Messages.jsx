import * as Loading from '../commJS/Loading.jsx';
import {Pagination} from '../commJS/Pagination.jsx';

(function ($, React, ReactDOM, i18n, axios, moment) {
    class MessagesBody extends React.Component {
        onGetMsgContent(msg){
            msg.flag=true;
            this.props.onGetMsgContent(msg.id);
        }

        render() {
            let data = this.props.Data;

            let messagesRows =  data.map(function( item ,index ) { 
                let createTime = moment(item.createTime).format('l LTS');
                return <div className="message-list__body" key={item.id}>
                            <div className="message" data-selected="false" data-read={item.flag} data-open={this.props.Open.indexOf(item.id)>-1}>
                                <div className="message__item">
                                    <label className="checkbox">
                                        <input type="checkbox" />
                                        <i className="icon icon-checkbox"></i>
                                    </label>
                                    <div className="message__title" onClick={this.onGetMsgContent.bind(this, item)}>
                                        <span className="text">{item.title}</span>
                                        <span className="text-lowlight">{createTime}</span>
                                    </div>
                                    <div className="btn-area btn-icon" onClick={this.onGetMsgContent.bind(this, item)}>
                                        <i className="icon icon-caret"></i>
                                    </div>
                                </div>
                                <div className="message__content">
                                    <p><pre>{this.props.Open.indexOf(item.id)>-1 ? this.props.MsgContent["MsgId"+item.id]:null}</pre></p>
                                </div>
                            </div>
                        </div>;
            }.bind(this));

            return (
                messagesRows
            )
        }
    }

    class Messages extends React.Component {
        constructor(props) {
            super(props);

            this.state={StartDate:"", EndDate:"", GetData: false, Refresh: false, Open:[], Checked:[]};
        }

        componentDidMount() {
            this.onGetMsgList(1);
        }

        onGetMsgList(pageNumber) {
            Loading.OnLoading();
            this.setState({Refresh: true});
            let data = {PageNumber: pageNumber};
            axios.post('/api/Account/GetMsgList', data)
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    this.setState({PageNumber: result.pageNumber, TotalPages: result.totalPages, Data: result.data, GetData: true, Refresh: false});
                } else {
                    alert(result.message);
                    this.setState({Refresh: false});
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();
                alert(error);
                this.setState({Refresh: false});
            }.bind(this));
        }

        onGetMsgContent(msgId) {
            let open = this.state.Open;
            let idx = open.indexOf(msgId);
            let msgContent = this.state.MsgContent||{};
            let key = "MsgId" + msgId;
            if(idx > -1) {
                open.splice(idx, 1);
                this.setState({open});
            } else {
                if(msgContent[key]==null){
                    Loading.OnLoading();
                    let data = {MsgId: msgId};
                    axios.post('/api/Account/GetMsgContent', data)
                    .then(function (response) {
                        Loading.UnLoading();
                        let result = response.data;
                        if (result.errorCode == 0) {
                            msgContent[key] = result.data.content;
                            open.push(msgId);
                            this.setState({MsgContent: msgContent, Open: open});
                        } else {
                            alert(result.message);
                        }
                    }.bind(this)).catch(function (error) {
                        Loading.UnLoading();
                        alert(error);
                    }.bind(this));
                } else {
                    open.push(msgId);
                    this.setState({Open: open});
                }
            }
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
                        <button className="btn btn-icon text-primary" data-refresh={this.state.Refresh} onClick={this.onGetMsgList.bind(this, this.state.PageNumber)}>
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
                </div>
                {this.state.Data ? <MessagesBody {...this.state} onGetMsgContent={this.onGetMsgContent.bind(this)}/> : null}
                <Pagination onPaging={this.onGetMsgList.bind(this)} PageNumber={this.state.PageNumber} TotalPages={this.state.TotalPages}/>
            </div>
            )
        }
    }
ReactDOM.render(<Messages/>,document.getElementById('Messages'));
})(jQuery, React, ReactDOM, i18n, axios, moment);