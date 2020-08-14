import React, { Component } from "react";
import Pagination from "../common/pagination";

class MessagesBody extends Component {
	onGetMsgContent(msg) {
		msg.flag = true;
		this.props.onGetMsgContent(msg.id);
	}

	onChecked(msg) {
		this.props.onChecked(msg.id);
	}

	render() {
		let data = this.props.Data;

		let messagesRows = data.map(
			function (item) {
				let createTime = moment(item.createTime).format("l LTS");
				return (
					<div className="msg-item" key={`personalMsg-${item.id}`}>
						<div className="checkbox">
							<input
								id={`mess-${item.id}`}
								type="checkbox"
								onChange={this.onChecked.bind(this, item)}
								checked={
									this.props.Checked.indexOf(item.id) > -1
								}
							/>
							<label htmlFor={`mess-${item.id}`}></label>
						</div>

						<div className="content">
							<div
								className={`title${
									item.flag ? "" : " unread"
								}`}>
								{item.title}
							</div>
							<div className="time">{createTime}</div>
						</div>
						<div
							className="action"
							onClick={this.onGetMsgContent.bind(this, item)}>
							<i className="icon icon-arrow-down"></i>
						</div>
						<div
							className={`description${
								this.props.Open.indexOf(item.id) > -1
									? ""
									: " hide"
							}`}>
							{this.props.Open.indexOf(item.id) > -1
								? this.props.MsgContent["MsgId" + item.id]
								: null}
						</div>
					</div>
				);
			}.bind(this)
		);

		return messagesRows;
	}
}

export default class Messages extends Component {
	constructor(props) {
		super(props);

		this.state = {
			StartDate: "",
			EndDate: "",
			GetData: false,
			Refresh: false,
			Open: [],
			Checked: []
		};
	}

	componentDidMount() {
		this.onGetMsgList(1);
	}

	onGetMsgList(pageNumber) {
		this.setState({ Refresh: true });
		let data = { PageNumber: pageNumber };
		axios
			.post("/api/Account/GetMsgList", data)
			.then(
				function (response) {
					let result = response.data;
					if (result.errorCode == 0) {
						this.setState({
							PageNumber: result.pageNumber,
							TotalPages: result.totalPages,
							Data: result.data,
							GetData: true,
							Refresh: false,
							Checked: []
						});
					} else {
						alert(result.message);
						this.setState({ Refresh: false });
					}
				}.bind(this)
			)
			.catch(
				function (error) {
					alert(error);
					this.setState({ Refresh: false });
				}.bind(this)
			);
	}

	onChecked(msgId) {
		let checked = this.state.Checked;
		let idx = checked.indexOf(msgId);
		if (idx > -1) {
			checked.splice(idx, 1);
			this.setState({ Checked: checked });
		} else {
			checked.push(msgId);
			this.setState({ Checked: checked });
		}
	}

	onGetMsgContent(msgId) {
		let open = this.state.Open;
		let idx = open.indexOf(msgId);
		let msgContent = this.state.MsgContent || {};
		let key = "MsgId" + msgId;
		if (idx > -1) {
			open.splice(idx, 1);
			this.setState({ Open: open });
		} else {
			if (msgContent[key] == null) {
				let data = { MsgId: msgId };
				axios
					.post("/api/Account/GetMsgContent", data)
					.then(
						function (response) {
							let result = response.data;
							if (result.errorCode == 0) {
								msgContent[key] = result.data.content;
								open.push(msgId);
								this.setState(
									{ MsgContent: msgContent, Open: open },
									function () {
										if (window.onGetMsgCount != null) {
											window.onGetMsgCount();
										}
									}
								);
							} else {
								alert(result.message);
							}
						}.bind(this)
					)
					.catch(
						function (error) {
							alert(error);
						}.bind(this)
					);
			} else {
				open.push(msgId);
				this.setState({ Open: open });
			}
		}
	}

	onDelMsg() {
		if (confirm(i18n.lbl_AreYouSure)) {
			axios
				.post("/api/Account/DelMsg", { MsgIds: this.state.Checked })
				.then(
					function (response) {
						let result = response.data;
						if (result.errorCode == 0) {
							this.onGetMsgList(this.state.PageNumber);
						} else {
							alert(result.message);
						}
					}.bind(this)
				)
				.catch(
					function (error) {
						alert(error);
					}.bind(this)
				);
		}
	}

	onSelAll() {
		let checked = [];
		if (
			this.state.Data != null &&
			this.state.Checked.length == this.state.Data.length
		) {
			this.setState({ Checked: checked });
		} else if (this.state.Data != null && this.state.Data.length > 0) {
			let _data = this.state.Data;

			for (let idx = 0; idx < _data.length; idx++) {
				checked.push(_data[idx].id);
			}

			this.setState({ Checked: checked });
		}
	}

	render() {
		return (
			<div className="content personal-block">
				<div className="option-header">
					<div className="checkbox">
						<input
							id="personalMsg-show-all"
							type="checkbox"
							checked={
								this.state.Data != null &&
								this.state.Data.length > 0 &&
								this.state.Checked.length ==
									this.state.Data.length
							}
							onChange={this.onSelAll.bind(this)}
						/>
						<label htmlFor="personalMsg-show-all">
							{i18n.lbl_SelectAll}
						</label>
					</div>
					<div className="action-group">
						<a
							onClick={
								this.state.Data != null &&
								this.state.Checked.length > 0
									? this.onDelMsg.bind(this)
									: null
							}>
							<i className="icon icon-delete"></i>
						</a>
						<a
							onClick={this.onGetMsgList.bind(
								this,
								this.state.PageNumber
							)}>
							<i className="icon icon-refresh-white"></i>
						</a>
					</div>
				</div>
				<div className="personal-msg">
					{this.state.Data ? (
						<MessagesBody
							{...this.state}
							onGetMsgContent={this.onGetMsgContent.bind(this)}
							onChecked={this.onChecked.bind(this)}
						/>
					) : null}
				</div>
				<Pagination
					onPaging={this.onGetMsgList.bind(this)}
					PageNumber={this.state.PageNumber}
					TotalPages={this.state.TotalPages}
				/>
			</div>
		);
	}
}

if (process.env.NODE_ENV === "production") {
	ReactDOM.render(<Messages />, document.getElementById("content"));
}
