import * as Loading from '../../commJS/Loading.jsx';
import {Pagination} from '../../commJS/Pagination.jsx';

(function ($, React, ReactDOM, i18n, axios, moment) {
    class MessagesBody extends React.Component {
        onGetMsgContent(msg){
            msg.flag=true;
            this.props.onGetMsgContent(msg.id);
        }

        onChecked(msg){
            this.props.onChecked(msg.id);
        }

        render() {
            let data = this.props.Data;

            let messagesRows =  data.map(function( item ,index ) { 
                let createTime = moment(item.createTime).format('l LTS');
                return <div className="message-list__body" key={item.id}>
                            <div className="message" data-selected="false" data-read={item.flag} data-open={this.props.Open.indexOf(item.id)>-1}>
                                <div className="message__item">
                                    <label className="checkbox">
                                        <input type="checkbox"  onChange={this.onChecked.bind(this, item)} checked={this.props.Checked.indexOf(item.id)>-1}/>
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
                                    {this.props.Open.indexOf(item.id)>-1 ? <pre><p>{this.props.MsgContent["MsgId"+item.id]}</p></pre> :null}
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
            //Loading.OnLoading();
            this.setState({Refresh: true});
            let data = {PageNumber: pageNumber};
            axios.post('/api/Account/GetMsgList', data)
            .then(function (response) {
                //Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    this.setState({PageNumber: result.pageNumber, TotalPages: result.totalPages, Data: result.data, GetData: true, Refresh: false, Checked: []});
                } else {
                    alert(result.message);
                    this.setState({Refresh: false});
                }
            }.bind(this)).catch(function (error) {
                //Loading.UnLoading();
                alert(error);
                this.setState({Refresh: false});
            }.bind(this));
        }

        onChecked(msgId) {
            let checked = this.state.Checked;
            let idx = checked.indexOf(msgId);
            if(idx > -1) {
                checked.splice(idx, 1);
                this.setState({Checked: checked});
            } else {
                checked.push(msgId);
                this.setState({Checked: checked});
            }
        }

        onGetMsgContent(msgId) {
            let open = this.state.Open;
            let idx = open.indexOf(msgId);
            let msgContent = this.state.MsgContent||{};
            let key = "MsgId" + msgId;
            if(idx > -1) {
                open.splice(idx, 1);
                this.setState({Open: open});
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
                            this.setState({MsgContent: msgContent, Open: open}, function(){
                                if(window.onGetMsgCount!=null) {
                                    window.onGetMsgCount();
                                }
                            });
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

        onDelMsg() {
            if(confirm(i18n.lbl_AreYouSure)) {
                Loading.OnLoading();
                axios.post('/api/Account/DelMsg', {MsgIds: this.state.Checked})
                .then(function (response) {
                    Loading.UnLoading();
                    let result = response.data;
                    if (result.errorCode == 0) {
                        this.onGetMsgList(this.state.PageNumber);
                    } else {
                        alert(result.message);
                    }
                }.bind(this)).catch(function (error) {
                    Loading.UnLoading();
                    alert(error);
                }.bind(this));
            }
        }

        onSelAll() {
            let checked=[];
            if(this.state.Data!=null&&this.state.Checked.length == this.state.Data.length) {
                this.setState({Checked: checked});
            } else if(this.state.Data!=null && this.state.Data.length>0 ) {
                let _data = this.state.Data;

                for(let idx = 0;idx < _data.length;idx++) {
                    checked.push(_data[idx].id)
                }

                this.setState({Checked: checked});
            }
        }

        render() {
            return (
            <div className="page">
                <div className="heading">
                    <span className="text">{i18n.lbl_PersonalMsg}</span>
                </div>
                <div className="message-list">
                    <div className="message-list__header">
                        <label className="checkbox">
                            <input type="checkbox" checked={this.state.Data!=null&&this.state.Checked.length == this.state.Data.length} onChange={this.onSelAll.bind(this)} />
                            <i className="icon icon-checkbox"></i>
                            <span className="text">{i18n.lbl_SelectAll}</span>
                        </label>
                        <button className="btn btn-icon text-primary" data-disabled={this.state.Data==null||this.state.Checked.length==0} onClick={this.state.Data!=null&&this.state.Checked.length > 0  ? this.onDelMsg.bind(this): null}>
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
                {this.state.Data ? <MessagesBody {...this.state} onGetMsgContent={this.onGetMsgContent.bind(this)} onChecked={this.onChecked.bind(this)}/> : null}
                <Pagination onPaging={this.onGetMsgList.bind(this)} PageNumber={this.state.PageNumber} TotalPages={this.state.TotalPages}/>
            </div>
            )
        }
    }
ReactDOM.render(<Messages/>,document.getElementById('Messages'));
})(jQuery, React, ReactDOM, i18n, axios, moment);