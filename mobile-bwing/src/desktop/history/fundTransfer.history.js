import React, { Component } from "react";
import { number_format } from "../../common/common";
import moment from "moment";

/* eslint-disable */
const TransferStatus = {
	0: <span>{i18n.lbl_Success}</span>,
	1: <span>{i18n.lbl_Fail}</span>,
	2: <span>{i18n.lbl_Pending}</span>,
	3: <span>{i18n.lbl_Pending}</span>
};
class HistoryBody extends Component {
	render() {
		let data = this.props.Data;
		let rows = data.map(
			function (item, index) {
				let dateMonent = moment(item.createTime);
				let direction = "";
				if (item.direction == 1) {
					direction = (
						<span>{`${i18n.lbl_MainWallet} > ${item.tpName}`}</span>
					);
				} else {
					direction = (
						<span>{`${item.tpName} > ${i18n.lbl_MainWallet}`}</span>
					);
				}

				return (
					<tr key={item.transId}>
						<td className="td-date">
							{dateMonent.format("l")} ({dateMonent.format("Z")})
						</td>
						<td className="td-time">
							{dateMonent.format("hh:mm:ss A")}
						</td>
						<td className="td-id">{item.transId}</td>
						<td className="td-method">{direction}</td>
						<td className="td-amount">
							{number_format(item.amount.toFixed(2))}
						</td>
						<td className="td-status">
							{TransferStatus[item.status]}
						</td>
					</tr>
				);
			}.bind(this)
		);

		let dateStr = moment(data[0].createTime).format("YYYY/MM/DD");
		return rows;
	}
}

export default class TransferHistory extends Component {
	constructor(props) {
		super(props);
	}

	render() {
		let data = this.props.Data;
		let bodyData = null;
		if (data != null) {
			let dataContainer = [];
			for (let i = 0; i < data.length; i++) {
				let row = data[i];
				let mDate = moment(row.createTime);
				let sDate = mDate.format("YYYYMMDD");
				if (
					dataContainer.length == 0 ||
					dataContainer[dataContainer.length - 1].Date != sDate
				) {
					dataContainer.push({ Date: sDate, Data: [row] });
				} else {
					dataContainer[dataContainer.length - 1].Data.push(row);
				}
			}

			bodyData = dataContainer.map(
				function (item, index) {
					return (
						<HistoryBody
							key={index}
							Data={item.Data}
							onShowBankModal={this.props.onShowBankModal}
						/>
					);
				}.bind(this)
			);
		}
		return data != null && data.length > 0 ? (
			<div className="table-content">
				<div className="option-header">
					<span className="text-bold">
						{i18n.lbl_TransactionHistory}
					</span>
				</div>
				<table>
					<thead>
						<tr>
							<th className="td-date">{i18n.lbl_Date}</th>
							<th className="td-time">{i18n.lbl_Time}</th>
							<th className="td-id">{i18n.lbl_ID}</th>
							<th className="td-method">{i18n.lbl_Direction}</th>
							<th className="td-amount">{i18n.lbl_Amount}</th>
							<th className="td-status">{i18n.lbl_Status}</th>
						</tr>
					</thead>
					<tbody>{bodyData}</tbody>
				</table>
			</div>
		) : (
			<div className="no-data">
				<span className="text">{i18n.msg_NoData}</span>
			</div>
		);
	}
}
/* eslint-enable */
