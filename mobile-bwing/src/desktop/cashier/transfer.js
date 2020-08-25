import React from "react";
import { number_format } from "../../common/common";
import DropdownList from "./transfer.dropdown";

const TransferStatus = {
	0: <span>{i18n.lbl_Success}</span>,
	1: <span>{i18n.lbl_Fail}</span>,
	2: <span>{i18n.lbl_Pending}</span>,
	3: <span>{i18n.lbl_Pending}</span>
};

class RecentHistoryBody extends React.Component {
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
					<tr key={`trans-${index}`}>
						<td>
							{dateMonent.format("l")} ({dateMonent.format("Z")})
						</td>
						<td>{dateMonent.format("hh:mm:ss A")}</td>
						<td>{item.transId}</td>
						<td>{direction}</td>
						<td>{number_format(item.amount.toFixed(2))}</td>
						<td>{TransferStatus[item.status]}</td>
					</tr>
				);
			}.bind(this)
		);

		let dateStr = moment(data[0].createTime).format("YYYY/MM/DD");
		return (
			<table>
				<thead>
					<tr>
						<th>{i18n.lbl_Date}</th>
						<th>{i18n.lbl_Time}</th>
						<th>{i18n.lbl_ID}</th>
						<th>{i18n.lbl_Method}</th>
						<th>{i18n.lbl_Amount}</th>
						<th>{i18n.lbl_Status}</th>
					</tr>
				</thead>
				<tbody>{rows}</tbody>
			</table>
		);
	}
}

class RecentHistory extends React.Component {
	constructor(props) {
		super(props);
	}

	componentDidMount() {
		this.GetRecent();
	}

	GetRecent() {
		this.props.GetRecent();
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
					return <RecentHistoryBody key={index} Data={item.Data} />;
				}.bind(this)
			);
		}

		return (
			<div className="table-content">
				<div className="option-header">
					<span className="text-bold">
						{i18n.lbl_LastTransaction}
					</span>
				</div>
				{bodyData}
				<div className="button-block">
					<button
						className="btn btn-primary"
						type="button"
						onClick={() => (window.location.href = hrefAllTrans)}>
						{i18n.lbl_AllTranscations}
					</button>
				</div>
			</div>
		);
	}
}

class TransferPanel extends React.Component {
	constructor(props) {
		super(props);
		this.state = {
			refresh: false,
			refreshRecent: false,
			WalletInfo: null,
			From: 0,
			To: 1,
			Amount: 1,
			showPopBox: false,
			turnover: 0,
			rollover: 0
		};
	}

	componentDidMount() {
		this.getWalletInfo();
	}

	getWalletInfo() {
		this.setState({ refresh: true });
		axios
			.post("/api/Account/GetWalletInfo")
			.then(
				function (response) {
					var result = response.data;
					if (result.errorCode != null && result.errorCode == 0) {
						this.setState(
							{ refresh: false, WalletInfo: result.data },
							function () {
								// if (setWalletInfo) {
								// 	setWalletInfo(result.data);
								// }

								$(".Js-Balance").html(
									number_format(result.data.balance)
								);
							}
						);
					} else {
						this.setState({ refresh: false });
					}
				}.bind(this)
			)
			.catch(
				function (error) {
					alert(error);
					this.setState({ refresh: false });
				}.bind(this)
			);
	}

	onSelectFrom(value) {
		let to = this.state.To;
		let from = this.state.From;
		if (value != from) {
			if (value == 0 && to == 0) {
				to = 1;
				this.setState({ From: value, To: to });
			} else {
				this.setState({ From: value, To: 0 });
			}
		}
	}

	onSelectTo(value) {
		let from = this.state.From;
		let to = this.state.To;
		if (value != to) {
			if (value == 0 && from == 0) {
				from = 1;
				this.setState({ To: value, From: from });
			} else {
				this.setState({ To: value, From: 0 });
			}
		}
	}

	GetRecent() {
		this.setState({ refreshRecent: true });
		axios
			.post("/api/Tp/GetTransferRecent")
			.then(
				function (response) {
					let result = response.data;
					if (result.errorCode == 0) {
						this.setState({
							Recent: result.data,
							refreshRecent: false
						});
					} else {
						this.setState({ refreshRecent: false });
						alert(result.message);
					}
				}.bind(this)
			)
			.catch(
				function (error) {
					this.setState({ refreshRecent: false });
					alert(error);
				}.bind(this)
			);
	}

	setHighestValue() {
		let WalletInfo = this.state.WalletInfo;
		let from = this.state.From;
		let amount = 0;
		if (WalletInfo != null) {
			if (from == 0) {
				amount = WalletInfo.balance;
			} else if (WalletInfo.tpWallets != null) {
				let data = this.state.WalletInfo.tpWallets;
				if (data != null) {
					for (let i = 0; i < data.length; i++) {
						if (data[i].tpId == from) {
							amount = data[i].balance;
							break;
						}
					}
				}
			}

			if (IsConvertRequired) {
				amount /= 1000;
			}
			if (amount > 1) {
				this.setState({ Amount: amount });
			} else {
				this.setState({ Amount: 1 });
			}
		}
	}

	handleChange(event) {
		var re = /^\+?[1-9][0-9]*$/;
		if (!isNaN(event.target.value) && re.test(event.target.value)) {
			this.setState({ Amount: parseFloat(event.target.value) });
		} else if (event.target.value == "") {
			this.setState({ Amount: "" });
		}
	}

	async handleSubmit(event) {
		event.preventDefault();
		let Amount = this.state.Amount;
		if (isNaN(Amount) || Amount < 1) {
		} else {
			let from = this.state.From;
			let to = this.state.To;
			let TpDirection = 2;
			let TpId = 0;
			if (from == 0) {
				TpDirection = 1;
				TpId = to;
			} else {
				TpId = from;
			}

			if (IsConvertRequired) {
				Amount *= 1000;
			}

			let data = {
				direction: TpDirection,
				amount: Amount,
				TpId: TpId
			};
			axios
				.post("/api/Tp/Transfer", data)
				.then(
					function (response) {
						let result = response.data;
						if (result.errorCode === 9) {
							this.setState({
								showPopBox: true,
								turnover: result.data.turnover,
								rollover: result.data.rollover
							});
						} else if (result.errorCode == 0) {
							this.getWalletInfo();
							this.GetRecent();
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

	onRetrieveAll() {
		axios
			.post("/api/Tp/RetrieveAll")
			.then(
				function (response) {
					let result = response.data;
					if (result.errorCode == 0) {
						this.getWalletInfo();
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

	onAllTransferIn(TpId) {
		axios
			.post("/api/Tp/AllTransferIn", { TpId: TpId })
			.then(
				function (response) {
					let result = response.data;
					if (result.errorCode == 0) {
						this.getWalletInfo();
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

	onAllTransferOut(TpId) {
		axios
			.post("/api/Tp/AllTransferOut", { TpId: TpId })
			.then(
				function (response) {
					let result = response.data;
					if (result.errorCode == 0) {
						this.getWalletInfo();
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

	ClosePopBox = () => {
		this.setState({ showPopBox: false });
	};

	render() {
		let WalletInfo = this.state.WalletInfo;
		let tpWallets = null;
		let data = [];
		data.push({
			Title: i18n.lbl_MainWallet,
			DisplayText:
				WalletInfo && number_format(WalletInfo.balance.toFixed(2)),
			Value: 0
		});

		if (WalletInfo != null && WalletInfo.tpWallets != null) {
			tpWallets = WalletInfo.tpWallets.map(
				function (item) {
					data.push({
						Title: item.tpName,
						DisplayText: number_format(item.balance.toFixed(2)),
						Value: item.tpId
					});

					return (
						<div className="wallet-group" key={item.tpId}>
							<span className="text-bold">{item.tpName}</span>
							<span className="text-bold text-green">
								{number_format(item.balance)}
							</span>
							<span
								className="icon-action"
								onClick={
									!this.state.refresh
										? this.getWalletInfo.bind(this)
										: null
								}>
								<i className="icon icon-refresh-white"></i>
							</span>
						</div>
					);
				}.bind(this)
			);
		}

		return (
			<>
				<div className="title">{i18n.lbl_Transfer}</div>
				<div className="content transferFunds-block">
					<div className="grid">
						<div className="wallet-group">
							<span className="text-bold">
								{i18n.lbl_MainWallet}
							</span>
							<span className="text-bold text-green">
								{WalletInfo &&
									number_format(
										WalletInfo.balance.toFixed(2)
									)}
							</span>
							<span
								className="icon-action"
								onClick={
									!this.state.refresh
										? this.getWalletInfo.bind(this)
										: null
								}>
								<i className="icon icon-refresh-white"></i>
							</span>
						</div>
						{tpWallets}
					</div>

					<div className="section">
						<button
							className="option-header toggle-section toggle-btn"
							data-toggle-content="section-method">
							<span className="text-bold">
								{i18n.lbl_TransferFunds}
							</span>
						</button>

						<div className="section-content" id="section-method">
							<div className="form">
								<div className="form-group">
									<div className="label">
										<span className="text-bold">
											{i18n.lbl_TransferFrom}
										</span>
									</div>
									<DropdownList
										Data={data}
										Value={this.state.From}
										onSelect={this.onSelectFrom.bind(this)}
									/>
									<div className="label">
										<span className="text-bold">
											{i18n.lbl_TransferTo}
										</span>
									</div>
									<DropdownList
										Data={data}
										Value={this.state.To}
										onSelect={this.onSelectTo.bind(this)}
									/>

									<div className="label">
										<span className="text-bold">
											{i18n.lbl_Amount}
										</span>
									</div>
									<div className="input-field">
										<input
											className="input-control text-green text-right"
											type="number"
											pattern="[0-9]*"
											value={this.state.Amount}
											onChange={this.handleChange.bind(
												this
											)}
										/>
										{IsConvertRequired ? (
											<span className="thousands text-green text-bold">
												('000)
											</span>
										) : null}
									</div>
								</div>
								<div className="button-group">
									<button
										className="btn btn-primary"
										onClick={this.handleSubmit.bind(this)}>
										{i18n.lbl_Transfer}
									</button>
									<button
										className="btn btn-secondary"
										onClick={this.setHighestValue.bind(
											this
										)}>
										{i18n.lbl_MaxAmount}
									</button>
								</div>
							</div>
						</div>
					</div>

					<RecentHistory
						GetRecent={this.GetRecent.bind(this)}
						Data={this.state.Recent}
						refresh={this.state.refreshRecent}
					/>
					{this.state.showPopBox ? (
						<TurnoverPopBox
							turnover={this.state.turnover}
							rollover={this.state.rollover}
							ClosePopBox={this.ClosePopBox}
						/>
					) : null}
				</div>
			</>
		);
	}
}

const TurnoverPopBox = ({ turnover, rollover, ClosePopBox }) => {
	let percentage =
		turnover > rollover ? 100 : ((turnover / rollover) * 100).toFixed(2);

	return (
		<div className="modal" data-open="true">
			<div className="overlay"></div>
			<div className="modal__dialog">
				<div className="modal__header">
					<span className="text">{i18n.lbl_ImportantNote}</span>
				</div>
				<div className="modal__body">
					<h3 className="schedule-txt">
						{i18n.lbl_TurnoverAccmulateHint}
					</h3>
					<div className="rollover-info">
						<div className="rollover-info__item">
							<div className="turnover">
								{i18n.lbl_TurnoverAccumulated}
							</div>
							<div className="schedule">
								<div
									className="schedule-bg"
									style={{ width: `${percentage}%` }}></div>
							</div>
							<span className="text">{percentage}%</span>
							<div className="schedule-info">
								<span className="text">
									{i18n.lbl_Currency}{" "}
									{number_format(turnover)}
								</span>{" "}
								/ ({i18n.lbl_Req}.{i18n.lbl_Currency}{" "}
								{number_format(rollover)})
							</div>
						</div>
					</div>
				</div>
				<div className="modal__footer">
					<div className="btn-group">
						<button
							className="btn btn-secondary"
							onClick={ClosePopBox}>
							<span className="text">OK</span>
						</button>
					</div>
				</div>
			</div>
		</div>
	);
};

export default TransferPanel;

if (process.env.NODE_ENV === "production") {
	ReactDOM.render(<TransferPanel />, document.getElementById("content"));
}
