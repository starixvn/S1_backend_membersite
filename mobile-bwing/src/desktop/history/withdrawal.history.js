import React, { Component } from "react";
import { number_format } from "../../common/common";

/* eslint-disable */
class HistoryBody extends Component {
	render() {
		let data = this.props.Data;
		let rows = data.map(
			function (item) {
				let dateMonent = moment(item.createTime);
				let statusGroup = null;

				if (
					item.status == 5 ||
					item.status == 6 ||
					item.status == 7 ||
					item.status == 99
				) {
					statusGroup = (
						<span>
							{i18n["lbl_WithdrawalStatus" + item.status]}
						</span>
					);
				} else {
					statusGroup = <span>{i18n.lbl_WithdrawalStatus4}</span>;
				}

				return (
					<tr key={item.transId}>
						<td>
							{dateMonent.format("l")} ({dateMonent.format("Z")})
						</td>
						<td>{dateMonent.format("hh:mm:ss A")}</td>
						<td>{item.transId}</td>
						<td>{number_format(item.amount.toFixed(2))}</td>
						<td>{statusGroup}</td>
					</tr>
				);
			}.bind(this)
		);

		let dateStr = moment(data[0].createTime).format("YYYY/MM/DD");
		return rows;
	}
}

export default class WithdrawalHistory extends Component {
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
							<th>{i18n.lbl_Date}</th>
							<th>{i18n.lbl_Time}</th>
							<th>{i18n.lbl_ID}</th>
							<th>{i18n.lbl_Amount}</th>
							<th>{i18n.lbl_Status}</th>
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
